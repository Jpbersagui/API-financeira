using System.Data;
using System.Text.Json;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Enums;
using ControleFinanceiro.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Services;

public sealed class FinanceiroException(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}

public class MovimentacaoService(AppDbContext db, TimeProvider clock, PrevisaoService previsoes)
{
    public DateOnly Hoje => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
    private static void Exigir(bool condition, string message, int status = 400)
    { if (!condition) throw new FinanceiroException(message, status); }
    private static void Versao(byte[] atual, string recebida) => Exigir(Convert.ToBase64String(atual) == recebida,
        "Este registro mudou. Atualize a tela antes de continuar.", 409);
    private void DataValida(DateOnly data) => Exigir(data != default && data <= Hoje, "Informe uma data válida que não esteja no futuro.");
    private static void ValorValido(decimal valor, bool abertura = false) => Exigir(
        Math.Abs(valor) <= 9999999999999999.99m && decimal.Round(valor, 2) == valor && (abertura || valor > 0),
        "Informe um valor válido com até duas casas decimais. Movimentos exigem valor positivo.");
    private static string Motivo(string motivo)
    {
        Exigir(!string.IsNullOrWhiteSpace(motivo) && motivo.Trim().Length <= 500, "Informe o motivo, com até 500 caracteres.");
        return motivo.Trim();
    }
    private async Task<T> Gravar<T>(Func<Task<T>> action)
    {
        // Serializa abertura/estado da conta com as confirmações que dependem deles.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var result = await action();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }
    private async Task<Conta> Conta(int id) => await db.Contas.FindAsync(id)
        ?? throw new FinanceiroException("Conta não encontrada.", 404);
    private async Task<Transacao> Transacao(int id) => await db.Transacoes.Include(t => t.Conta).FirstOrDefaultAsync(t => t.Id == id)
        ?? throw new FinanceiroException("Lançamento não encontrado.", 404);

    public static bool EstruturaCredito(Transacao t) => t.OrigemRegistro == OrigemRegistroTransacao.CreditoLegado
        || t.MetodoPagamento == MetodoPagamento.CartaoCredito || t.NumeroParcelas > 1
        || t.ParcelaAtual != null || t.TransacaoOrigemId != null;
    public async Task<bool> Credito(Transacao t) => EstruturaCredito(t) || await db.Transacoes.AnyAsync(p => p.TransacaoOrigemId == t.Id);
    private async Task Elegivel(Transacao t)
    {
        Exigir(!await Credito(t), "Crédito legado não pode ser confirmado como movimentação bancária.");
        Exigir(t.ClassificacaoPendente == null, "Transferência própria aguarda classificação em uma fase futura.");
        Exigir(Enum.IsDefined(t.Tipo) && Enum.IsDefined(t.MetodoPagamento), "Tipo ou método inválido.");
        ValorValido(t.Valor);
    }

    public Task<Conta> Abertura(int id, AberturaContaDto dto) => Gravar(async () =>
    {
        var conta = await Conta(id); Versao(conta.Versao, dto.Versao);
        var data = dto.DataAbertura!.Value; DataValida(data); ValorValido(dto.ValorAbertura!.Value, true);
        if (conta.DataAbertura is DateOnly antiga && data > antiga)
        {
            var afetados = await db.Transacoes.Where(t => t.ContaId == id && t.Estado == EstadoTransacao.Confirmada
                && t.DataEfetivacao >= antiga && t.DataEfetivacao < data).Select(t => t.Id).ToListAsync();
            Exigir(afetados.Count == 0, $"Revise os movimentos confirmados antes de avançar a abertura. Registros afetados: {string.Join(", ", afetados)}.", 409);
        }
        conta.DataAbertura = data; conta.ValorAbertura = dto.ValorAbertura;
        return conta;
    });

    private void Preencher(Transacao t, TransacaoRealizadaCreateDto dto)
    {
        DataValida(dto.DataEfetivacao!.Value); ValorValido(dto.Valor!.Value);
        Exigir(dto.MetodoPagamento != MetodoPagamento.CartaoCredito, "Compras de crédito continuam no cadastro legado.");
        t.Titulo = dto.Titulo.Trim(); t.Valor = dto.Valor.Value; t.Tipo = dto.Tipo!.Value;
        t.Categoria = dto.Categoria.Trim(); t.MetodoPagamento = dto.MetodoPagamento!.Value;
        t.DataEfetivacao = dto.DataEfetivacao;
    }

    public Task<Transacao> Criar(TransacaoRealizadaCreateDto dto) => Gravar(() => PrepararMovimento(dto));

    private async Task<Transacao> PrepararMovimento(TransacaoRealizadaCreateDto dto)
    {
        var conta = await Conta(dto.ContaId!.Value);
        Exigir(conta.Ativa, "Novos movimentos exigem conta ativa.");
        Exigir(conta.DataAbertura != null, "Defina o saldo de abertura desta conta antes de registrar movimentos.");
        Exigir(dto.DataEfetivacao >= conta.DataAbertura, "Novo movimento deve estar no período acompanhado. Use revisão para histórico antigo.");
        var t = new Transacao { Conta = conta, OrigemRegistro = OrigemRegistroTransacao.Realizada,
            Estado = EstadoTransacao.Confirmada, ConfirmadaEm = clock.GetUtcNow() };
        Preencher(t, dto);
        t.Data = dto.DataEfetivacao!.Value.ToDateTime(TimeOnly.MinValue);
        db.Transacoes.Add(t); return t;
    }

    public Task<Transacao> RealizarPrevisao(int id, RealizarPrevisaoDto dto) => Gravar(async () =>
    {
        var p = await previsoes.Obter(id);
        PrevisaoService.Versao(p.Versao, dto.Versao); PrevisaoService.Ativa(p);
        Exigir(p.Tipo == dto.Tipo, "O tipo da movimentação deve corresponder à previsão.");
        var antes = PrevisaoService.Snapshot(p);
        var t = await PrepararMovimento(dto);
        t.Previsao = p; t.PrevisaoId = p.Id;
        previsoes.InvalidarVersao(p);
        // Salva para obter o Id, ainda dentro da mesma transação. Falhas posteriores revertem tudo.
        await db.SaveChangesAsync();
        previsoes.Revisao(p, "Realizacao", antes, $"Movimentação {t.Id} registrada e vinculada");
        return t;
    });

    public Task<Transacao> Confirmar(int id, ConfirmarTransacaoDto dto) => Gravar(async () =>
    {
        var t = await Transacao(id); await Elegivel(t); DataValida(dto.DataEfetivacao!.Value);
        if (t.Estado == EstadoTransacao.Confirmada)
        {
            Exigir(t.ContaId == dto.ContaId && t.DataEfetivacao == dto.DataEfetivacao, "Confirmação já realizada com outros dados. Use correção.", 409);
            return t;
        }
        Versao(t.Versao, dto.Versao);
        Exigir(t.Estado == EstadoTransacao.NaoReconciliada, "Registro desconsiderado não pode ser confirmado.", 409);
        t.Conta = await Conta(dto.ContaId!.Value);
        t.DataEfetivacao = dto.DataEfetivacao; t.ConfirmadaEm = clock.GetUtcNow(); t.Estado = EstadoTransacao.Confirmada;
        return t;
    });

    private static string Snapshot(Transacao t) => JsonSerializer.Serialize(new {
        t.Titulo, t.Valor, t.Data, t.DataEfetivacao, t.ContaId, t.Tipo, t.Categoria,
        t.MetodoPagamento, t.Estado, t.ConfirmadaEm, t.DesconsideradaEm, t.MotivoDesconsideracao, t.PrevisaoId
    });
    private void Revisao(Transacao t, string antes, string motivo) => db.RevisoesTransacoes.Add(new() {
        Transacao = t, Instante = clock.GetUtcNow(), Motivo = motivo, Antes = antes, Depois = Snapshot(t)
    });

    public Task<Transacao> Corrigir(int id, CorrigirTransacaoDto dto) => Gravar(async () =>
    {
        var t = await Transacao(id); Versao(t.Versao, dto.Versao);
        Exigir(t.Estado == EstadoTransacao.Confirmada, "A correção financeira exige um movimento confirmado.", 409);
        await Elegivel(t); var motivo = Motivo(dto.Motivo); var antes = Snapshot(t);
        if (t.PrevisaoId is int previsaoId)
        {
            var p = await previsoes.Obter(previsaoId);
            Exigir(p.Tipo == dto.Tipo, "Remova o vínculo com a previsão antes de alterar o tipo.");
            previsoes.InvalidarVersao(p);
        }
        t.Conta = await Conta(dto.ContaId!.Value); t.ContaId = t.Conta.Id;
        Preencher(t, dto); // Data legada e instante original de confirmação são preservados.
        Revisao(t, antes, motivo); return t;
    });

    public Task<Transacao> Desconsiderar(int id, DesconsiderarTransacaoDto dto) => Gravar(async () =>
    {
        var t = await Transacao(id); Versao(t.Versao, dto.Versao); var motivo = Motivo(dto.Motivo);
        Exigir(t.Estado != EstadoTransacao.Desconsiderada, "Registro já desconsiderado.", 409);
        var antes = Snapshot(t);
        if (t.PrevisaoId is int previsaoId) previsoes.InvalidarVersao(await previsoes.Obter(previsaoId));
        t.Estado = EstadoTransacao.Desconsiderada; t.DesconsideradaEm = clock.GetUtcNow(); t.MotivoDesconsideracao = motivo;
        Revisao(t, antes, motivo); return t;
    });

    public Task<Transacao> Classificar(int id, ClassificarHistoricoDto dto) => Gravar(async () =>
    {
        var t = await Transacao(id); Versao(t.Versao, dto.Versao);
        Exigir(t.Estado == EstadoTransacao.NaoReconciliada, "Somente histórico não reconciliado pode receber classificação pendente.", 409);
        t.ClassificacaoPendente = dto.TransferenciaPropria!.Value ? ClassificacaoPendenteTransacao.TransferenciaPropria : null;
        return t;
    });
}
