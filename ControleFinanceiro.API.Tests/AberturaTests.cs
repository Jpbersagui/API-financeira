using System.Net;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Services;
using Xunit;
namespace ControleFinanceiro.API.Tests;
public class AberturaTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private readonly Fase2Scenario s = new(fixture);
    [Fact]
    public async Task DatasFinanceirasUsamCalendarioLocalNaViradaUtc()
    {
        var conta = await s.Conta();
        await using var db = fixture.OpenDb();
        var clock = new ViradaClock();
        var movimentos = new MovimentacaoService(db, clock, new PrevisaoService(db, clock));
        var saldos = new SaldoService(db, clock);
        Assert.Equal(new DateOnly(2026, 10, 31), movimentos.Hoje);
        Assert.Equal(movimentos.Hoje, saldos.Hoje);
        var abertura = await movimentos.Abertura(Fase2Scenario.Id(conta), new AberturaContaDto {
            DataAbertura = new(2026, 10, 31), ValorAbertura = 0, Versao = Fase2Scenario.Version(conta) });
        await Assert.ThrowsAsync<FinanceiroException>(() => movimentos.Abertura(abertura.Id, new AberturaContaDto {
            DataAbertura = new(2026, 11, 1), ValorAbertura = 0, Versao = Convert.ToBase64String(abertura.Versao) }));
    }
    private sealed class ViradaClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 1, 1, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("Teste -03", TimeSpan.FromHours(-3), "Teste", "Teste");
    }
    [Theory]
    [InlineData(850)] [InlineData(0)] [InlineData(-850)]
    public async Task AberturaNaoEhReceitaENaoPresumeSinal(int valor)
    {
        var c = await s.Conta(valor); var id = Fase2Scenario.Id(c);
        Assert.Equal(valor, (await s.Saldo(id)).GetProperty("saldoCalculado").GetDecimal());
        var ex = await s.Read($"/api/contas/{id}/extrato?inicio=2026-08-01&fim=2026-08-31");
        Assert.Empty(ex.GetProperty("movimentos").EnumerateArray());
        Assert.Equal(valor, ex.GetProperty("saldoInicial").GetDecimal());
        Assert.True(ex.GetProperty("exibirAbertura").GetBoolean());
    }
    [Fact]
    public async Task SemAberturaOuAntesDelaNaoHaSaldo()
    {
        var c = await s.Conta(); var id = Fase2Scenario.Id(c);
        Assert.False((await s.Saldo(id)).GetProperty("calculavel").GetBoolean());
        await s.Abrir(c, 100, "2026-08-10");
        Assert.False((await s.Saldo(id, "2026-08-09")).GetProperty("calculavel").GetBoolean());
    }
    [Theory]
    [InlineData("2026-11-01")] [InlineData("0001-01-01")]
    public async Task AberturaRejeitaDataInvalidaOuFutura(string data) => await s.Abrir(await s.Conta(), 0, data, HttpStatusCode.BadRequest);
    [Fact]
    public async Task AberturaExigeDoisCamposERejeitaCentavosInvalidos()
    {
        var c = await s.Conta();
        await s.Send($"/api/contas/{Fase2Scenario.Id(c)}/abertura", new { dataAbertura = "2026-08-01", versao = Fase2Scenario.Version(c) }, HttpMethod.Put, HttpStatusCode.BadRequest);
        await s.Abrir(c, 0.001m, "2026-08-01", HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task CorrecaoDataBloqueiaExclusaoAteRevisaoExplicita()
    {
        var c = await s.Conta(100); var t = await s.Realizada(Fase2Scenario.Id(c));
        await s.Abrir(c, 200, "2026-08-10", HttpStatusCode.Conflict);
        await s.Desconsiderar(t);
        var nova = await s.Abrir(c, 200, "2026-08-10");
        Assert.Equal(200, (await s.Saldo(Fase2Scenario.Id(c))).GetProperty("saldoCalculado").GetDecimal());
        await s.Abrir(c, 0, "2026-08-01", HttpStatusCode.Conflict);
    }
}
