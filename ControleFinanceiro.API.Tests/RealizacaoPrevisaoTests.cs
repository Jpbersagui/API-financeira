using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Enums;
using ControleFinanceiro.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using static ControleFinanceiro.API.Tests.Fase2Scenario;
namespace ControleFinanceiro.API.Tests;
public class RealizacaoPrevisaoTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private readonly PrevisaoScenario s = new(fixture);
    [Fact]
    public async Task SalarioParcialMultiploIntegralEExcedenteEmContasDiferentes()
    {
        var c = await s.Conta(850); var outra = await s.Conta(0);
        var p = await s.Previsao(contaId: Id(c));
        p = await s.Realizar(p, Id(outra), 1000);
        Assert.Equal(1000, p.GetProperty("valorRealizado").GetDecimal()); Assert.Equal(2000, p.GetProperty("valorRestante").GetDecimal());
        Assert.Equal("ParcialmenteRealizada", p.GetProperty("situacao").GetString());
        p = await s.Realizar(p, Id(c), 2000);
        Assert.Equal("Realizada", p.GetProperty("situacao").GetString());
        p = await s.Realizar(p, Id(c), 20);
        Assert.Equal(20, p.GetProperty("excedente").GetDecimal()); Assert.Equal(-20, p.GetProperty("diferenca").GetDecimal());
        Assert.Equal(3, p.GetProperty("transacoes").GetArrayLength());
        Assert.Equal(2870, (await s.Saldo(Id(c))).GetProperty("saldoCalculado").GetDecimal());
        Assert.Equal(1000, (await s.Saldo(Id(outra))).GetProperty("saldoCalculado").GetDecimal());
    }
    [Fact]
    public async Task ContaVariavelEEncerramentoDivergente()
    {
        var c = await s.Conta(1000); var p = await s.Previsao(250, "Despesa");
        p = await s.Final(p, 273.18m); p = await s.Realizar(p, Id(c), 273.18m);
        Assert.Equal(0, p.GetProperty("valorRestante").GetDecimal()); Assert.Equal(250, p.GetProperty("valorPrevistoOriginal").GetDecimal());
        var q = await s.Previsao(250, "Despesa"); q = await s.Realizar(q, Id(c), 230);
        await s.Estado(q, "cancelamento", expected: HttpStatusCode.Conflict);
        q = await s.Estado(q, "encerramento"); Assert.Equal(20, q.GetProperty("diferenca").GetDecimal());
        Assert.Equal(0, q.GetProperty("valorRestante").GetDecimal());
        await s.Realizar(q, Id(c), 20, expected: HttpStatusCode.Conflict);
        q = await s.Estado(q, "reabertura"); Assert.Equal(20, q.GetProperty("valorRestante").GetDecimal());
    }
    [Fact]
    public async Task SemAberturaInativaEDataFuturaNaoCriamTransacao()
    {
        var c = await s.Conta(); var p = await s.Previsao(contaId: Id(c));
        await s.Realizar(p, Id(c), 10, expected: HttpStatusCode.BadRequest);
        c = await s.Abrir(c, 0, "2026-08-01");
        await s.Realizar(p, Id(c), 10, "2027-01-01", HttpStatusCode.BadRequest);
        await s.Realizar(p, Id(c), 10, "2026-07-31", HttpStatusCode.BadRequest);
        await s.Send($"/api/contas/{Id(c)}", new { nome = "Inativa", tipo = "ContaCorrente", ativa = false }, HttpMethod.Put);
        await s.Realizar(p, Id(c), 10, expected: HttpStatusCode.BadRequest);
        var atual = await s.Read($"/api/previsoes/{Id(p)}");
        Assert.Empty(atual.GetProperty("transacoes").EnumerateArray()); Assert.Equal(Version(p), Version(atual));
    }
    [Fact]
    public async Task CorrecaoEDesconsideracaoRecalculamSemReabrirEncerrada()
    {
        var c = await s.Conta(1000); var p = await s.Previsao(250, "Despesa");
        p = await s.Realizar(p, Id(c), 230); var t = p.GetProperty("transacoes")[0]; p = await s.Estado(p, "encerramento");
        var versaoAnterior = Version(p);
        await s.Send($"/api/transacoes/{Id(t)}/correcao", new { titulo = "Corrigida", valor = 200, tipo = "Receita",
            contaId = Id(c), categoria = "Outros", dataEfetivacao = "2026-08-05", metodoPagamento = "Pix", motivo = "Correção", versao = Version(t) }, HttpMethod.Put, HttpStatusCode.BadRequest);
        t = await s.Send($"/api/transacoes/{Id(t)}/correcao", new { titulo = "Corrigida", valor = 200, tipo = "Despesa",
            contaId = Id(c), categoria = "Outros", dataEfetivacao = "2026-08-05", metodoPagamento = "Pix", motivo = "Correção", versao = Version(t) }, HttpMethod.Put);
        p = await s.Read($"/api/previsoes/{Id(p)}");
        Assert.NotEqual(versaoAnterior, Version(p)); Assert.Equal(200, p.GetProperty("valorRealizado").GetDecimal());
        Assert.Equal("Encerrada", p.GetProperty("estado").GetString());
        await s.Desconsiderar(t); p = await s.Read($"/api/previsoes/{Id(p)}");
        Assert.Equal(0, p.GetProperty("valorRealizado").GetDecimal()); Assert.Equal(0, p.GetProperty("valorRestante").GetDecimal());
        Assert.Single(p.GetProperty("transacoes").EnumerateArray());
        p = await s.Estado(p, "reabertura"); Assert.Equal(250, p.GetProperty("valorRestante").GetDecimal());
    }
    [Fact]
    public async Task ConcorrenciaNaoDuplicaRealizacaoEVersaoConsumidaConflita()
    {
        var c = await s.Conta(0); var p = await s.Previsao(100);
        var data = new { titulo = "Concorrente", valor = 40, tipo = "Receita", contaId = Id(c), dataEfetivacao = "2026-08-05",
            categoria = "Outros", metodoPagamento = "Pix", versao = Version(p) };
        var respostas = await Task.WhenAll(s.Client.PostAsJsonAsync($"/api/previsoes/{Id(p)}/realizacoes", data),
            s.Client.PostAsJsonAsync($"/api/previsoes/{Id(p)}/realizacoes", data));
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Conflict);
        await s.Realizar(p, Id(c), 40, expected: HttpStatusCode.Conflict);
        p = await s.Read($"/api/previsoes/{Id(p)}");
        Assert.Single(p.GetProperty("transacoes").EnumerateArray()); Assert.Equal(40, p.GetProperty("valorRealizado").GetDecimal());
        Assert.Equal(40, (await s.Saldo(Id(c))).GetProperty("saldoCalculado").GetDecimal());
    }
    [Fact]
    public async Task FalhaNaRevisaoReverteMovimentoVinculoEVersao()
    {
        var c = await s.Conta(0); var p = await s.Previsao(100);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(fixture.ConnectionString)
            .AddInterceptors(new FalhaRevisao()).Options;
        await using var db = new AppDbContext(options); var clock = new TestClock();
        var servico = new MovimentacaoService(db, clock, new PrevisaoService(db, clock));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.RealizarPrevisao(Id(p), new RealizarPrevisaoDto {
            Titulo = "Não deve persistir", Valor = 40, Tipo = TipoTransacao.Receita, ContaId = Id(c),
            DataEfetivacao = new(2026,8,5), Categoria = "Outros", MetodoPagamento = MetodoPagamento.Pix, Versao = Version(p) }));
        var atual = await s.Read($"/api/previsoes/{Id(p)}");
        Assert.Equal(Version(p), Version(atual)); Assert.Empty(atual.GetProperty("transacoes").EnumerateArray());
        Assert.Equal(0, (await s.Saldo(Id(c))).GetProperty("saldoCalculado").GetDecimal());
        Assert.Single((await s.Read($"/api/previsoes/{Id(p)}/revisoes")).EnumerateArray());
    }
    private class FalhaRevisao : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<ControleFinanceiro.API.Models.RevisaoPrevisao>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Falha simulada depois de gravar o movimento, antes do commit.");
            return ValueTask.FromResult(result);
        }
    }
}
