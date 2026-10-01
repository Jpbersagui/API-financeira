using System.Net;
using Xunit;
namespace ControleFinanceiro.API.Tests;
public class SaldoTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private readonly Fase2Scenario s = new(fixture);
    [Fact]
    public async Task SaldoUsaSomenteConfirmadosElegiveisDesdeAbertura()
    {
        var c = await s.Conta(850); int id = Fase2Scenario.Id(c);
        await s.Realizada(id, 1000, "Receita", "2026-08-01"); await s.Realizada(id, 80);
        await s.Legado(id); await s.Legado(id, "CartaoCredito");
        var anterior = await s.Legado(id); await s.Confirmar(anterior, id, "2026-07-31");
        var incorreto = await s.Realizada(id, 50); await s.Desconsiderar(incorreto);
        Assert.Equal(1770, (await s.Saldo(id)).GetProperty("saldoCalculado").GetDecimal());
        Assert.Equal(1850, (await s.Saldo(id, "2026-08-01")).GetProperty("saldoCalculado").GetDecimal());
    }
    [Fact]
    public async Task ConsolidadoIncluiInativasEListaExcluidas()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c);
        await s.Send($"/api/contas/{id}", new { nome = "Inativa", tipo = "Carteira", ativa = false }, HttpMethod.Put);
        var sem = await s.Conta(); var consolidado = await s.Read("/api/contas/saldos?data=2026-08-31");
        Assert.True(consolidado.GetProperty("parcial").GetBoolean());
        Assert.Contains(consolidado.GetProperty("contasExcluidas").EnumerateArray(), x => x.GetProperty("contaId").GetInt32() == Fase2Scenario.Id(sem));
        var contas = consolidado.GetProperty("contas").EnumerateArray().ToList();
        Assert.Equal(contas.Where(x => x.GetProperty("calculavel").GetBoolean()).Sum(x => x.GetProperty("saldoCalculado").GetDecimal()), consolidado.GetProperty("totalCalculavel").GetDecimal());
        Assert.Contains(contas, x => x.GetProperty("contaId").GetInt32() == id && !x.GetProperty("ativa").GetBoolean() && x.GetProperty("saldoCalculado").GetDecimal() == 100);
    }
    [Fact]
    public async Task NovoRealizadoExigeAberturaContaAtivaEPeriodo()
    {
        var c = await s.Conta(); int id = Fase2Scenario.Id(c);
        await s.Realizada(id, expected: HttpStatusCode.BadRequest);
        await s.Abrir(c, 100, "2026-08-01");
        await s.Realizada(id, data: "2026-07-31", expected: HttpStatusCode.BadRequest);
        await s.Realizada(id, data: "2026-11-01", expected: HttpStatusCode.BadRequest);
        await s.Send($"/api/contas/{id}", new { nome = "Inativa", tipo = "Carteira", ativa = false }, HttpMethod.Put);
        await s.Realizada(id, expected: HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task DashboardSeparaLegadoAtualEPeriodoFuturo()
    {
        var atual = await s.Read("/api/dashboard?mes=8&ano=2026");
        Assert.False(atual.GetProperty("saldoBancarioConfirmado").GetBoolean());
        Assert.Equal("2026-10-31", atual.GetProperty("financeiro").GetProperty("atual").GetProperty("referencia").GetString());
        Assert.Equal("2026-08-31", atual.GetProperty("financeiro").GetProperty("fimPeriodo").GetString());
        var futuro = await s.Read("/api/dashboard?mes=11&ano=2026");
        Assert.False(futuro.GetProperty("financeiro").TryGetProperty("posicaoPeriodo", out _));
    }
}
