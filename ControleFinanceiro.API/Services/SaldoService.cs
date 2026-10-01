using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Enums;
using ControleFinanceiro.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Services;

public class SaldoService(AppDbContext db, TimeProvider clock)
{
    public DateOnly Hoje => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
    private void Referencia(DateOnly data)
    { if (data == default || data > Hoje) throw new FinanceiroException("Informe uma referência válida até hoje. Projeções não estão disponíveis."); }
    private IQueryable<Transacao> Elegiveis => db.Transacoes.AsNoTracking().Where(t =>
        t.Estado == EstadoTransacao.Confirmada && t.ContaId != null && t.DataEfetivacao != null
        && t.ClassificacaoPendente == null && t.OrigemRegistro != OrigemRegistroTransacao.CreditoLegado
        && t.MetodoPagamento != MetodoPagamento.CartaoCredito && t.NumeroParcelas == 1
        && t.ParcelaAtual == null && t.TransacaoOrigemId == null && !t.Parcelas.Any());
    private static Task<decimal> Total(IQueryable<Transacao> query) => query.SumAsync(t => t.Tipo == TipoTransacao.Receita ? t.Valor : -t.Valor);

    public async Task<SaldoContaDto> Conta(int id, DateOnly data)
    {
        Referencia(data);
        var c = await db.Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id) ?? throw new FinanceiroException("Conta não encontrada.", 404);
        var result = new SaldoContaDto { ContaId = c.Id, Nome = c.Nome, Ativa = c.Ativa, Referencia = data, DataAbertura = c.DataAbertura,
            PendentesRevisao = await db.Transacoes.CountAsync(t => t.ContaId == id && t.Estado == EstadoTransacao.NaoReconciliada) };
        if (c.DataAbertura == null) result.MotivoIndisponibilidade = "Defina o saldo inicial.";
        else if (data < c.DataAbertura) result.MotivoIndisponibilidade = "Período não acompanhado.";
        else result.SaldoCalculado = c.ValorAbertura!.Value + await Total(Elegiveis.Where(t => t.ContaId == id && t.DataEfetivacao >= c.DataAbertura && t.DataEfetivacao <= data));
        return result;
    }

    public async Task<SaldoConsolidadoDto> Consolidado(DateOnly data)
    {
        Referencia(data);
        var result = new SaldoConsolidadoDto { Referencia = data,
            PendentesRevisao = await db.Transacoes.CountAsync(t => t.Estado == EstadoTransacao.NaoReconciliada) };
        foreach (var id in await db.Contas.OrderBy(c => c.Nome).ThenBy(c => c.Id).Select(c => c.Id).ToListAsync())
            result.Contas.Add(await Conta(id, data));
        return result;
    }

    public async Task<ExtratoContaDto> Extrato(int id, DateOnly inicio, DateOnly fim)
    {
        Referencia(inicio); Referencia(fim);
        if (inicio > fim) throw new FinanceiroException("O início deve ser anterior ou igual ao fim.");
        var c = await db.Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id) ?? throw new FinanceiroException("Conta não encontrada.", 404);
        var result = new ExtratoContaDto { ContaId = id, Inicio = inicio, Fim = fim, DataAbertura = c.DataAbertura, ValorAbertura = c.ValorAbertura };
        if (c.DataAbertura == null || fim < c.DataAbertura)
        { result.Aviso = c.DataAbertura == null ? "Defina o saldo inicial." : "Período não acompanhado."; return result; }
        var efetivo = inicio > c.DataAbertura.Value ? inicio : c.DataAbertura.Value;
        result.InicioAcompanhado = efetivo; result.ExibirAbertura = inicio <= c.DataAbertura && c.DataAbertura <= fim;
        if (inicio < efetivo) result.Aviso = "O trecho anterior à abertura não é acompanhado.";
        var query = Elegiveis.Where(t => t.ContaId == id && t.DataEfetivacao >= c.DataAbertura);
        decimal saldo = c.ValorAbertura!.Value + await Total(query.Where(t => t.DataEfetivacao < efetivo));
        result.SaldoInicial = saldo;
        foreach (var t in await query.Where(t => t.DataEfetivacao >= efetivo && t.DataEfetivacao <= fim)
            .OrderBy(t => t.DataEfetivacao).ThenBy(t => t.Id).ToListAsync())
        {
            var entrada = t.Tipo == TipoTransacao.Receita ? t.Valor : 0;
            var saida = t.Tipo == TipoTransacao.Despesa ? t.Valor : 0;
            saldo += entrada - saida;
            result.Movimentos.Add(new() { Id = t.Id, DataEfetivacao = t.DataEfetivacao!.Value, Descricao = t.Titulo, Entrada = entrada, Saida = saida, Saldo = saldo });
        }
        result.SaldoFinal = saldo; return result;
    }

    public async Task<ResumoFinanceiroDto> Resumo(int mes, int ano)
    {
        if (mes is < 1 or > 12 || ano is < 1900 or > 2100) throw new FinanceiroException("Mês ou ano inválido.");
        var inicio = new DateOnly(ano, mes, 1);
        var result = new ResumoFinanceiroDto { Atual = await Consolidado(Hoje), InicioPeriodo = inicio };
        if (inicio > Hoje) return result;
        var fim = inicio.AddMonths(1).AddDays(-1); if (fim > Hoje) fim = Hoje;
        result.FimPeriodo = fim; result.PosicaoPeriodo = await Consolidado(fim);
        var query = Elegiveis.Where(t => t.Conta!.DataAbertura != null && t.DataEfetivacao >= t.Conta.DataAbertura && t.DataEfetivacao >= inicio && t.DataEfetivacao <= fim);
        result.EntradasConfirmadas = await query.Where(t => t.Tipo == TipoTransacao.Receita).SumAsync(t => t.Valor);
        result.SaidasConfirmadas = await query.Where(t => t.Tipo == TipoTransacao.Despesa).SumAsync(t => t.Valor);
        return result;
    }
}
