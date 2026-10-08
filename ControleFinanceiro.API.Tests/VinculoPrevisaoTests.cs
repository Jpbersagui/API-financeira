using System.Net;
using System.Net.Http.Json;
using Xunit;
using static ControleFinanceiro.API.Tests.Fase2Scenario;
namespace ControleFinanceiro.API.Tests;
public class VinculoPrevisaoTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private readonly PrevisaoScenario s = new(fixture);
    [Fact]
    public async Task VinculoUnicoIdempotenteDesvinculoPreservaSaldoETransacao()
    {
        var c = await s.Conta(1000); var t = await s.Realizada(Id(c), 80); var p = await s.Previsao(80, "Despesa");
        var original = p; p = await s.Vincular(p, t); await s.Vincular(original, t);
        Assert.Equal("Realizada", p.GetProperty("situacao").GetString());
        Assert.Equal(2, (await s.Read($"/api/previsoes/{Id(p)}/revisoes")).GetArrayLength());
        var q = await s.Previsao(80, "Despesa"); t = await s.Read($"/api/transacoes/{Id(t)}");
        await s.Vincular(q, t, HttpStatusCode.Conflict);
        var saldo = (await s.Saldo(Id(c))).GetProperty("saldoCalculado").GetDecimal();
        p = await s.Desvincular(p, t);
        Assert.Equal(80, p.GetProperty("valorRestante").GetDecimal());
        t = await s.Read($"/api/transacoes/{Id(t)}"); Assert.False(t.TryGetProperty("previsaoId", out _));
        Assert.Equal("Confirmada", t.GetProperty("estado").GetString());
        Assert.Equal(saldo, (await s.Saldo(Id(c))).GetProperty("saldoCalculado").GetDecimal());
        await s.Vincular(q, t);
    }
    [Fact]
    public async Task HistoricoAnteriorAberturaEmContaInativaQuitaSemAlterarSaldo()
    {
        var c = await s.Conta(850, "2026-09-01"); var t = await s.Legado(Id(c));
        await s.Send($"/api/contas/{Id(c)}", new { nome = "Histórica", tipo = "ContaCorrente", ativa = false }, HttpMethod.Put);
        t = await s.Confirmar(t, Id(c)); var p = await s.Previsao(80, "Despesa");
        p = await s.Vincular(p, t); Assert.Equal(80, p.GetProperty("valorRealizado").GetDecimal());
        Assert.Equal(0, p.GetProperty("valorRestante").GetDecimal());
        Assert.Equal(850, (await s.Saldo(Id(c), "2026-09-30")).GetProperty("saldoCalculado").GetDecimal());
        var ex = await s.Read($"/api/contas/{Id(c)}/extrato?inicio=2026-09-01&fim=2026-09-30");
        Assert.Empty(ex.GetProperty("movimentos").EnumerateArray());
    }
    [Fact]
    public async Task BloqueiaNaoReconciliadaCreditoTransferenciaDesconsideradaETipoIncompativel()
    {
        var c = await s.Conta(1000); var p = await s.Previsao(80, "Despesa");
        var t = await s.Legado(Id(c)); await s.Vincular(p, t, HttpStatusCode.BadRequest);
        var credito = await s.Legado(Id(c), "CartaoCredito"); await s.Vincular(p, credito, HttpStatusCode.BadRequest);
        t = await s.Send($"/api/transacoes/{Id(t)}/classificacao", new { transferenciaPropria = true, versao = Version(t) }, HttpMethod.Put);
        await s.Vincular(p, t, HttpStatusCode.BadRequest);
        var realizada = await s.Realizada(Id(c), tipo: "Receita"); await s.Vincular(p, realizada, HttpStatusCode.BadRequest);
        realizada = await s.Realizada(Id(c)); realizada = await s.Desconsiderar(realizada);
        await s.Vincular(p, realizada, HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task TerminalBloqueiaVinculoMasPermiteDesvincularSemReabrir()
    {
        var c = await s.Conta(1000); var t = await s.Realizada(Id(c)); var p = await s.Previsao(80, "Despesa");
        p = await s.Estado(p, "cancelamento"); await s.Vincular(p, t, HttpStatusCode.Conflict);
        p = await s.Estado(p, "reabertura"); p = await s.Vincular(p, t);
        p = await s.Estado(p, "encerramento"); t = await s.Read($"/api/transacoes/{Id(t)}"); p = await s.Desvincular(p, t);
        Assert.Equal("Encerrada", p.GetProperty("estado").GetString()); Assert.Equal(0, p.GetProperty("valorRestante").GetDecimal());
        Assert.Equal(80, p.GetProperty("diferenca").GetDecimal());
    }
    [Fact]
    public async Task DesconsiderarRealizacaoAtivaReabreRestanteSemAlterarDecisao()
    {
        var c = await s.Conta(0); var p = await s.Previsao(80); p = await s.Realizar(p, Id(c), 80);
        var versao = Version(p); await s.Desconsiderar(p.GetProperty("transacoes")[0]);
        p = await s.Read($"/api/previsoes/{Id(p)}");
        Assert.NotEqual(versao, Version(p)); Assert.Equal("Ativa", p.GetProperty("estado").GetString());
        Assert.Equal(80, p.GetProperty("valorRestante").GetDecimal());
        p = await s.Estado(p, "cancelamento"); Assert.Equal("Cancelada", p.GetProperty("estado").GetString());
    }
    [Fact]
    public async Task DuasPrevisoesConcorrentesNaoCompartilhamUmaTransacao()
    {
        var c = await s.Conta(0); var t = await s.Realizada(Id(c), tipo: "Receita");
        var p = await s.Previsao(80); var q = await s.Previsao(80);
        var respostas = await Task.WhenAll(
            s.Client.PutAsJsonAsync($"/api/previsoes/{Id(p)}/transacoes/{Id(t)}", new { versao = Version(p), versaoTransacao = Version(t) }),
            s.Client.PutAsJsonAsync($"/api/previsoes/{Id(q)}/transacoes/{Id(t)}", new { versao = Version(q), versaoTransacao = Version(t) }));
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Conflict);
        p = await s.Read($"/api/previsoes/{Id(p)}"); q = await s.Read($"/api/previsoes/{Id(q)}");
        Assert.Equal(80, p.GetProperty("valorRealizado").GetDecimal() + q.GetProperty("valorRealizado").GetDecimal());
    }
}
