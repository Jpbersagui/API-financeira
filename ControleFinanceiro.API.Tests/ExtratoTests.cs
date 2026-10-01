using System.Net;
using Xunit;
namespace ControleFinanceiro.API.Tests;
public class ExtratoTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private readonly Fase2Scenario s = new(fixture);
    [Fact]
    public async Task ExtratoOrdenaPorDataEIdECalculaAnteriorAoIntervalo()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c);
        var segundoDia = await s.Realizada(id, 10, data: "2026-08-05");
        await s.Realizada(id, 50, "Receita", "2026-08-02");
        var mesmoDia = await s.Realizada(id, 20, data: "2026-08-05");
        await s.Legado(id);
        var d = await s.Read($"/api/contas/{id}/extrato?inicio=2026-08-05&fim=2026-08-05");
        Assert.Equal(150, d.GetProperty("saldoInicial").GetDecimal()); Assert.Equal(120, d.GetProperty("saldoFinal").GetDecimal());
        var linhas = d.GetProperty("movimentos"); Assert.Equal(2, linhas.GetArrayLength());
        Assert.Equal(Fase2Scenario.Id(segundoDia), linhas[0].GetProperty("id").GetInt32()); Assert.Equal(Fase2Scenario.Id(mesmoDia), linhas[1].GetProperty("id").GetInt32());
        Assert.Equal(140, linhas[0].GetProperty("saldo").GetDecimal());
    }
    [Fact]
    public async Task ExtratoTrataTrechoAnteriorEAusenciaDeMovimentos()
    {
        int id = Fase2Scenario.Id(await s.Conta(100, "2026-08-10"));
        var anterior = await s.Read($"/api/contas/{id}/extrato?inicio=2026-08-01&fim=2026-08-09");
        Assert.False(anterior.TryGetProperty("saldoInicial", out _));
        var parcial = await s.Read($"/api/contas/{id}/extrato?inicio=2026-08-01&fim=2026-08-31");
        Assert.Equal("2026-08-10", parcial.GetProperty("inicioAcompanhado").GetString());
        Assert.Equal(100, parcial.GetProperty("saldoFinal").GetDecimal());
        Assert.Empty(parcial.GetProperty("movimentos").EnumerateArray());
    }
    [Fact]
    public async Task ExtratoRejeitaIntervaloInvertidoEFuturo()
    {
        int id = Fase2Scenario.Id(await s.Conta(100));
        Assert.Equal(HttpStatusCode.BadRequest, (await s.Client.GetAsync($"/api/contas/{id}/extrato?inicio=2026-08-10&fim=2026-08-01")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.Client.GetAsync($"/api/contas/{id}/extrato?inicio=2026-08-01&fim=2026-11-01")).StatusCode);
    }
}
