using System.Net;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Enums;
using ControleFinanceiro.API.Models;
using ControleFinanceiro.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static ControleFinanceiro.API.Tests.Fase2Scenario;
namespace ControleFinanceiro.API.Tests;
public class PrevisaoTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private readonly PrevisaoScenario s = new(fixture);
    [Theory]
    [InlineData("Receita")]
    [InlineData("Despesa")]
    public async Task CadastroSemContaNaoAfetaSaldo(string tipo)
    {
        var c = await s.Conta(850); var antes = await s.Saldo(Id(c));
        var p = await s.Previsao(250, tipo);
        Assert.False(p.TryGetProperty("contaId", out _));
        Assert.Equal(250, p.GetProperty("valorRestante").GetDecimal());
        Assert.Equal("Aberta", p.GetProperty("situacao").GetString());
        Assert.Equal(antes.GetProperty("saldoCalculado").GetDecimal(), (await s.Saldo(Id(c))).GetProperty("saldoCalculado").GetDecimal());
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public async Task RejeitaOriginalInvalido(decimal valor)
    {
        await s.Send("/api/previsoes", new { descricao = "Inválida", valorPrevistoOriginal = valor, tipo = "Despesa", dataPrevista = "2027-01-01", categoria = "Outros" }, expected: HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task OriginalImutavelFinalZeroESemExclusao()
    {
        var p = await s.Previsao(); p = await s.Final(p, 0);
        Assert.Equal(3000, p.GetProperty("valorPrevistoOriginal").GetDecimal());
        Assert.Equal("SemValor", p.GetProperty("situacao").GetString());
        Assert.False(p.GetProperty("vencida").GetBoolean());
        Assert.Empty(p.GetProperty("transacoes").EnumerateArray());
        await s.Send($"/api/previsoes/{Id(p)}", new { descricao = "Alterar", dataPrevista = "2026-09-01", categoria = "Outros",
            versao = Version(p), valorPrevistoOriginal = 1 }, HttpMethod.Put, HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await s.Client.DeleteAsync($"/api/previsoes/{Id(p)}")).StatusCode);
        await s.Final(p, -1, HttpStatusCode.BadRequest);
        await s.Final(p, 1.001m, HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task CompetenciaIndependenteSemUnicidadeEValidacoes()
    {
        for (int i = 0; i < 2; i++)
        {
            var p = await s.Send("/api/previsoes", new { descricao = "Competência", valorPrevistoOriginal = 10, tipo = "Despesa",
                dataPrevista = "2027-01-31", categoria = "Outros", anoCompetencia = 2026, mesCompetencia = 9 }, expected: HttpStatusCode.Created);
            Assert.Equal(9, p.GetProperty("mesCompetencia").GetInt32()); Assert.False(p.GetProperty("vencida").GetBoolean());
        }
        foreach (var mes in new int?[] { null, 0, 13 })
            await s.Send("/api/previsoes", new { descricao = "Inválida", valorPrevistoOriginal = 10, tipo = "Despesa",
                dataPrevista = "2026-01-31", categoria = "Outros", anoCompetencia = 2026, mesCompetencia = mes }, expected: HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task EncerramentoSemRealizacaoCancelamentoReaberturaEMotivo()
    {
        var p = await s.Previsao(250);
        await s.Estado(p, "encerramento", "", HttpStatusCode.BadRequest);
        p = await s.Estado(p, "encerramento");
        Assert.Equal(0, p.GetProperty("valorRestante").GetDecimal()); Assert.Equal(250, p.GetProperty("diferenca").GetDecimal());
        await s.Final(p, 200, HttpStatusCode.Conflict);
        await s.Estado(p, "reabertura", "", HttpStatusCode.BadRequest);
        p = await s.Estado(p, "reabertura"); Assert.Equal(250, p.GetProperty("valorRestante").GetDecimal());
        p = await s.Estado(p, "cancelamento"); Assert.Equal("Cancelada", p.GetProperty("situacao").GetString());
        p = await s.Estado(p, "reabertura"); Assert.False(p.TryGetProperty("motivoCancelamento", out _));
        var h = await s.Read($"/api/previsoes/{Id(p)}/revisoes"); Assert.Equal(5, h.GetArrayLength());
    }
    [Fact]
    public async Task PlanejarSemAberturaPreservarContaInativaEEditarSemReescreverOriginal()
    {
        var c = await s.Conta(); var p = await s.Previsao(contaId: Id(c));
        await s.Send($"/api/contas/{Id(c)}", new { nome = "Inativa", tipo = "ContaCorrente", ativa = false }, HttpMethod.Put);
        await s.Send("/api/previsoes", new { descricao = "Inválida", valorPrevistoOriginal = 10, tipo = "Receita", dataPrevista = "2026-10-01", categoria = "Outros", contaId = Id(c) }, expected: HttpStatusCode.BadRequest);
        p = await s.Send($"/api/previsoes/{Id(p)}", new { descricao = "Atualizada", categoria = "Nova", contaId = Id(c), dataPrevista = "2026-12-01", versao = Version(p) }, HttpMethod.Put);
        Assert.Equal("Atualizada", p.GetProperty("descricao").GetString()); Assert.Equal(3000, p.GetProperty("valorPrevistoOriginal").GetDecimal());
        Assert.False(p.GetProperty("contaAtiva").GetBoolean());
    }
    [Fact]
    public async Task DashboardFiltraDataPrevistaSituacaoAtualNaoFluxoDeCaixa()
    {
        var c = await s.Conta(100); var p = await s.Previsao(300, data: "2026-01-03");
        await s.Realizar(p, Id(c), 100, "2026-10-02");
        var cancelada = await s.Previsao(500, data: "2026-01-04"); await s.Estado(cancelada, "cancelamento");
        var d = await s.Read("/api/dashboard?ano=2026&mes=1");
        var resumo = d.GetProperty("previsoes");
        Assert.Equal(300, resumo.GetProperty("receitas").GetProperty("original").GetDecimal());
        Assert.Equal(100, resumo.GetProperty("receitas").GetProperty("realizado").GetDecimal());
        Assert.Equal(1, resumo.GetProperty("canceladas").GetInt32());
        Assert.True(d.TryGetProperty("financeiro", out _)); Assert.True(d.TryGetProperty("previsaoProximosMeses", out _));
        var filtro = await s.Read("/api/previsoes?inicio=2026-01-01&fim=2026-01-31&situacao=ParcialmenteRealizada&vencida=true");
        Assert.Single(filtro.EnumerateArray());
    }
    [Fact]
    public void VencimentoUsaCalendarioLocalSemProcessoContinuo()
    {
        using var db = fixture.OpenDb();
        var clock = new RelogioPrevisao();
        var service = new PrevisaoService(db, clock);
        var p = new Previsao { ValorPrevistoOriginal = 20, DataPrevista = new(2026, 12, 31) };
        Assert.False(service.Mapear(p).Vencida);
        clock.Instante = new(2027, 1, 1, 4, 0, 0, TimeSpan.Zero);
        Assert.True(service.Mapear(p).Vencida);
        p.Estado = EstadoPrevisao.Encerrada; Assert.False(service.Mapear(p).Vencida);
    }
    private class RelogioPrevisao : TimeProvider
    {
        public DateTimeOffset Instante = new(2027, 1, 1, 1, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Instante;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("Teste", TimeSpan.FromHours(-3), "Teste", "Teste");
    }
}
