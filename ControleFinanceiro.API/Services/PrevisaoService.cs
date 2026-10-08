using System.Data;
using System.Text.Json;
using ControleFinanceiro.API.Controllers;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Enums;
using ControleFinanceiro.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Services;

public class PrevisaoService(AppDbContext db, TimeProvider clock)
{
    public DateOnly Hoje => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
    private static void Exigir(bool ok, string mensagem, int status = 400)
    { if (!ok) throw new FinanceiroException(mensagem, status); }
    internal static void Versao(byte[] atual, string recebida) =>
        Exigir(Convert.ToBase64String(atual) == recebida, "A previsão ou movimentação mudou. Atualize a tela.", 409);
    private static void Valor(decimal valor, bool zero = false) =>
        Exigir(valor >= (zero ? 0 : 0.01m) && valor <= 9999999999999999.99m && decimal.Round(valor, 2) == valor,
            "Informe um valor válido com até duas casas decimais.");
    private static string Motivo(string motivo)
    {
        Exigir(!string.IsNullOrWhiteSpace(motivo) && motivo.Trim().Length <= 500, "Informe um motivo com até 500 caracteres.");
        return motivo.Trim();
    }
    private async Task<T> Gravar<T>(Func<Task<T>> action)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var result = await action();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }
    internal async Task<Previsao> Obter(int id) => await db.Previsoes.Include(p => p.Conta)
        .Include(p => p.Transacoes).ThenInclude(t => t.Conta)
        .Include(p => p.Transacoes).ThenInclude(t => t.Parcelas)
        .FirstOrDefaultAsync(p => p.Id == id) ?? throw new FinanceiroException("Previsão não encontrada.", 404);
    internal static void Ativa(Previsao p) => Exigir(p.Estado == EstadoPrevisao.Ativa, "Reabra a previsão antes desta ação.", 409);
    // Força um UPDATE protegido pela rowversion mesmo quando somente um participante muda.
    internal void InvalidarVersao(Previsao p) => db.Entry(p).Property(x => x.Descricao).IsModified = true;
    private static bool Participa(Previsao p, Transacao t) => t.Estado == EstadoTransacao.Confirmada
        && t.Tipo == p.Tipo && t.ContaId != null && t.DataEfetivacao != null && t.Valor > 0
        && !MovimentacaoService.EstruturaCredito(t) && t.Parcelas.Count == 0 && t.ClassificacaoPendente == null;
    internal static string Snapshot(Previsao p) => JsonSerializer.Serialize(new {
        p.Descricao, p.Tipo, p.ValorPrevistoOriginal, p.ValorFinal, p.DataPrevista, p.ContaId,
        p.Categoria, p.Observacoes, p.AnoCompetencia, p.MesCompetencia, p.Estado,
        p.EncerradaEm, p.CanceladaEm, p.MotivoEncerramento, p.MotivoCancelamento,
        Transacoes = p.Transacoes.Select(t => t.Id).Order().ToArray()
    });
    internal void Revisao(Previsao p, string acao, string antes, string motivo) =>
        db.RevisoesPrevisoes.Add(new() { Previsao = p, Instante = clock.GetUtcNow(),
            Acao = acao, Motivo = motivo, Antes = antes, Depois = Snapshot(p) });
    private async Task Planejamento(Previsao p, string descricao, DateOnly data, int? contaId,
        string categoria, string? observacoes, int? ano, int? mes)
    {
        Exigir(data != default, "Informe a data prevista.");
        Exigir(ano.HasValue == mes.HasValue && (!ano.HasValue || (ano >= 1 && ano <= 9999 && mes >= 1 && mes <= 12)),
            "Informe ano e mês da competência juntos.");
        if (contaId != null && (p.Id == 0 || contaId != p.ContaId))
            Exigir(await db.Contas.AnyAsync(c => c.Id == contaId && c.Ativa), "Escolha uma conta planejada ativa ou deixe sem conta.");
        p.Descricao = descricao.Trim(); p.DataPrevista = data; p.ContaId = contaId;
        p.Categoria = categoria.Trim(); p.Observacoes = observacoes?.Trim(); p.AnoCompetencia = ano; p.MesCompetencia = mes;
    }
    public Task<Previsao> Criar(PrevisaoCreateDto dto) => Gravar(async () =>
    {
        Valor(dto.ValorPrevistoOriginal!.Value);
        if (dto.ValorFinal.HasValue) Valor(dto.ValorFinal.Value, true);
        var p = new Previsao { Tipo = dto.Tipo!.Value, ValorPrevistoOriginal = dto.ValorPrevistoOriginal.Value, ValorFinal = dto.ValorFinal };
        await Planejamento(p, dto.Descricao, dto.DataPrevista!.Value, dto.ContaId, dto.Categoria, dto.Observacoes, dto.AnoCompetencia, dto.MesCompetencia);
        db.Previsoes.Add(p); Revisao(p, "Criacao", "{}", "Previsão cadastrada"); return p;
    });
    public Task<Previsao> Editar(int id, PrevisaoUpdateDto dto) => Gravar(async () =>
    {
        var p = await Obter(id); Versao(p.Versao, dto.Versao); Ativa(p); var antes = Snapshot(p);
        await Planejamento(p, dto.Descricao, dto.DataPrevista!.Value, dto.ContaId, dto.Categoria, dto.Observacoes, dto.AnoCompetencia, dto.MesCompetencia);
        InvalidarVersao(p); Revisao(p, "Edicao", antes, "Dados de planejamento atualizados"); return p;
    });
    public Task<Previsao> DefinirFinal(int id, DefinirValorFinalDto dto) => Gravar(async () =>
    {
        var p = await Obter(id); Versao(p.Versao, dto.Versao); Ativa(p); Valor(dto.ValorFinal!.Value, true);
        var antes = Snapshot(p); p.ValorFinal = dto.ValorFinal; InvalidarVersao(p);
        Revisao(p, "ValorFinal", antes, "Valor final informado"); return p;
    });
    public Task<Previsao> AlterarEstado(int id, AlterarEstadoPrevisaoDto dto, EstadoPrevisao estado) => Gravar(async () =>
    {
        var p = await Obter(id); Versao(p.Versao, dto.Versao); var motivo = Motivo(dto.Motivo);
        if (estado == EstadoPrevisao.Ativa) Exigir(p.Estado != EstadoPrevisao.Ativa, "A previsão já está ativa.", 409);
        else Ativa(p);
        if (estado == EstadoPrevisao.Cancelada)
            Exigir(!p.Transacoes.Any(t => Participa(p, t)), "Há realização confirmada. Use Encerrar restante.", 409);
        var antes = Snapshot(p); p.Estado = estado;
        p.EncerradaEm = estado == EstadoPrevisao.Encerrada ? clock.GetUtcNow() : null;
        p.CanceladaEm = estado == EstadoPrevisao.Cancelada ? clock.GetUtcNow() : null;
        p.MotivoEncerramento = estado == EstadoPrevisao.Encerrada ? motivo : null;
        p.MotivoCancelamento = estado == EstadoPrevisao.Cancelada ? motivo : null;
        Revisao(p, estado == EstadoPrevisao.Ativa ? "Reabertura" : estado == EstadoPrevisao.Encerrada ? "Encerramento" : "Cancelamento", antes, motivo);
        return p;
    });
    public Task<Previsao> Vincular(int id, int transacaoId, VincularTransacaoDto dto) => Gravar(async () =>
    {
        var p = await Obter(id);
        var t = await db.Transacoes.Include(t => t.Parcelas).FirstOrDefaultAsync(t => t.Id == transacaoId)
            ?? throw new FinanceiroException("Movimentação não encontrada.", 404);
        if (t.PrevisaoId == id) return p; // repetição não cria participação nem revisão duplicadas
        Versao(p.Versao, dto.Versao); Versao(t.Versao, dto.VersaoTransacao); Ativa(p);
        Exigir(t.PrevisaoId == null, "Remova o vínculo anterior antes de vincular a outra previsão.", 409);
        Exigir(Participa(p, t), "Escolha uma movimentação confirmada do mesmo tipo, sem crédito ou transferência própria.");
        var antes = Snapshot(p); t.Previsao = p; t.PrevisaoId = id;
        if (!p.Transacoes.Contains(t)) p.Transacoes.Add(t);
        InvalidarVersao(p); Revisao(p, "Vinculacao", antes, $"Movimentação {t.Id} vinculada"); return p;
    });
    public Task<Previsao> Desvincular(int id, int transacaoId, DesvincularTransacaoDto dto) => Gravar(async () =>
    {
        var p = await Obter(id); Versao(p.Versao, dto.Versao); var motivo = Motivo(dto.Motivo);
        var t = p.Transacoes.FirstOrDefault(t => t.Id == transacaoId)
            ?? throw new FinanceiroException("Esta movimentação não está vinculada à previsão.", 409);
        Versao(t.Versao, dto.VersaoTransacao); var antes = Snapshot(p);
        t.PrevisaoId = null; t.Previsao = null; p.Transacoes.Remove(t);
        InvalidarVersao(p); Revisao(p, "Desvinculacao", antes, motivo); return p;
    });
    public PrevisaoReadDto Mapear(Previsao p)
    {
        var referencia = p.ValorFinal ?? p.ValorPrevistoOriginal;
        var realizado = p.Transacoes.Where(t => Participa(p, t)).Sum(t => t.Valor);
        var diferenca = referencia - realizado;
        var restante = p.Estado == EstadoPrevisao.Ativa ? Math.Max(diferenca, 0) : 0;
        var situacao = p.Estado == EstadoPrevisao.Cancelada ? SituacaoPrevisao.Cancelada
            : p.Estado == EstadoPrevisao.Encerrada ? SituacaoPrevisao.Encerrada
            : referencia == 0 && realizado == 0 ? SituacaoPrevisao.SemValor
            : restante == 0 ? SituacaoPrevisao.Realizada
            : realizado > 0 ? SituacaoPrevisao.ParcialmenteRealizada : SituacaoPrevisao.Aberta;
        return new() { Id = p.Id, Descricao = p.Descricao, Tipo = p.Tipo, ValorPrevistoOriginal = p.ValorPrevistoOriginal,
            ValorFinal = p.ValorFinal, ValorReferencia = referencia, ValorRealizado = realizado, ValorRestante = restante,
            Diferenca = diferenca, Excedente = Math.Max(-diferenca, 0), DataPrevista = p.DataPrevista, ContaId = p.ContaId,
            ContaNome = p.Conta?.Nome, ContaAtiva = p.Conta?.Ativa, Categoria = p.Categoria, Observacoes = p.Observacoes,
            AnoCompetencia = p.AnoCompetencia, MesCompetencia = p.MesCompetencia, Estado = p.Estado, Situacao = situacao,
            Vencida = p.DataPrevista < Hoje && restante > 0, MotivoEncerramento = p.MotivoEncerramento,
            MotivoCancelamento = p.MotivoCancelamento, Versao = Convert.ToBase64String(p.Versao),
            Transacoes = p.Transacoes.OrderBy(t => t.DataEfetivacao).ThenBy(t => t.Id).Select(TransacoesController.MapToReadDto).ToList() };
    }
    public async Task<PrevisaoReadDto> Consultar(int id) => Mapear(await Obter(id));
    public async Task<List<PrevisaoReadDto>> Listar(PrevisaoFiltroDto filtro)
    {
        Exigir(filtro.Inicio == null || filtro.Fim == null || filtro.Inicio <= filtro.Fim, "Intervalo de datas inválido.");
        var query = db.Previsoes.AsNoTracking().Include(p => p.Conta).Include(p => p.Transacoes).ThenInclude(t => t.Conta)
            .Include(p => p.Transacoes).ThenInclude(t => t.Parcelas).AsQueryable();
        if (filtro.Inicio != null) query = query.Where(p => p.DataPrevista >= filtro.Inicio);
        if (filtro.Fim != null) query = query.Where(p => p.DataPrevista <= filtro.Fim);
        if (filtro.ContaId != null) query = query.Where(p => p.ContaId == filtro.ContaId);
        if (filtro.SemConta == true) query = query.Where(p => p.ContaId == null);
        if (filtro.Tipo != null) query = query.Where(p => p.Tipo == filtro.Tipo);
        var result = (await query.OrderBy(p => p.DataPrevista).ThenBy(p => p.Id).ToListAsync()).Select(Mapear);
        if (filtro.Situacao != null) result = result.Where(p => p.Situacao == filtro.Situacao);
        if (filtro.Vencida != null) result = result.Where(p => p.Vencida == filtro.Vencida);
        return result.ToList();
    }
    public async Task<ResumoPrevisoesDto> Resumo(int ano, int mes)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var lista = await Listar(new() { Inicio = inicio, Fim = new DateOnly(ano, mes, DateTime.DaysInMonth(ano, mes)) });
        TotaisPrevisoesDto Total(TipoTransacao tipo)
        {
            var itens = lista.Where(p => p.Tipo == tipo && p.Estado != EstadoPrevisao.Cancelada).ToList();
            return new(itens.Sum(p => p.ValorPrevistoOriginal), itens.Sum(p => p.ValorReferencia),
                itens.Sum(p => p.ValorRealizado), itens.Sum(p => p.ValorRestante), itens.Sum(p => p.Diferenca),
                itens.Sum(p => p.Excedente), itens.Count(p => p.Vencida));
        }
        return new(ano, mes, Total(TipoTransacao.Receita), Total(TipoTransacao.Despesa), lista.Count(p => p.Estado == EstadoPrevisao.Cancelada));
    }
    public async Task<List<RevisaoPrevisaoDto>> Historico(int id)
    {
        Exigir(await db.Previsoes.AnyAsync(p => p.Id == id), "Previsão não encontrada.", 404);
        return await db.RevisoesPrevisoes.AsNoTracking().Where(r => r.PrevisaoId == id).OrderByDescending(r => r.Id)
            .Select(r => new RevisaoPrevisaoDto(r.Id, r.Instante, r.Acao, r.Motivo, r.Antes, r.Depois)).ToListAsync();
    }
}
