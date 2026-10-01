using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace ControleFinanceiro.API.Tests;
public class ReconciliacaoTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private readonly Fase2Scenario s = new(fixture);
    [Fact]
    public async Task CorrecaoEntreContasRecalculaAmbasEPreservaRevisao()
    {
        int origem = Fase2Scenario.Id(await s.Conta(100)), destino = Fase2Scenario.Id(await s.Conta(200));
        var t = await s.Realizada(origem, 20);
        await s.Send($"/api/transacoes/{Fase2Scenario.Id(t)}/correcao", new {
            titulo = "Conta corrigida", valor = 20, contaId = destino, tipo = "Despesa", categoria = "Outros",
            metodoPagamento = "Pix", dataEfetivacao = "2026-08-05", motivo = "Conta errada", versao = Fase2Scenario.Version(t)
        }, HttpMethod.Put);
        Assert.Equal(100, (await s.Saldo(origem)).GetProperty("saldoCalculado").GetDecimal());
        Assert.Equal(180, (await s.Saldo(destino)).GetProperty("saldoCalculado").GetDecimal());
        var revisoes = await s.Read($"/api/transacoes/{Fase2Scenario.Id(t)}/revisoes");
        using var antes = System.Text.Json.JsonDocument.Parse(revisoes[0].GetProperty("antes").GetString()!);
        using var depois = System.Text.Json.JsonDocument.Parse(revisoes[0].GetProperty("depois").GetString()!);
        Assert.Equal(origem, antes.RootElement.GetProperty("ContaId").GetInt32());
        Assert.Equal(destino, depois.RootElement.GetProperty("ContaId").GetInt32());
    }
    [Fact]
    public async Task ConfirmacaoIdempotenteEContaInativaPreservamDataAntiga()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c); var t = await s.Legado(id);
        await s.Send($"/api/contas/{id}", new { nome = "Antiga", tipo = "Carteira", ativa = false }, HttpMethod.Put);
        var confirmado = await s.Confirmar(t, id); await s.Confirmar(t, id);
        Assert.Equal(t.GetProperty("data").GetString(), confirmado.GetProperty("data").GetString());
        Assert.Equal(20, (await s.Saldo(id)).GetProperty("saldoCalculado").GetDecimal());
        await s.Confirmar(t, id, "2026-08-06", HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task AssociacaoNaoConfirmaEConfirmacaoExigeDados()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c);
        var old = await s.Read("/api/transacoes/101");
        await s.Send("/api/transacoes/101/conta", new { contaId = id }, HttpMethod.Patch);
        Assert.Equal(100, (await s.Saldo(id)).GetProperty("saldoCalculado").GetDecimal());
        await s.Confirmar(old, id, expected: HttpStatusCode.Conflict);
        await s.Send("/api/transacoes/101/confirmacao", new { contaId = id }, expected: HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task CorrecaoEspecificaGuardaRevisaoENaoMudaDataLegada()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c); var t = await s.Realizada(id, 10);
        await s.Send($"/api/contas/{id}", new { nome = "Antiga", tipo = "Carteira", ativa = false }, HttpMethod.Put);
        var data = new { titulo = "Corrigido", valor = 20, contaId = id, tipo = "Despesa", categoria = "Outros", metodoPagamento = "Pix", dataEfetivacao = "2026-08-06", motivo = "Valor digitado incorretamente", versao = Fase2Scenario.Version(t) };
        var corrigido = await s.Send($"/api/transacoes/{Fase2Scenario.Id(t)}/correcao", data, HttpMethod.Put);
        Assert.Equal(t.GetProperty("data").GetString(), corrigido.GetProperty("data").GetString());
        Assert.Equal(80, (await s.Saldo(id)).GetProperty("saldoCalculado").GetDecimal());
        var revisoes = await s.Read($"/api/transacoes/{Fase2Scenario.Id(t)}/revisoes");
        Assert.Single(revisoes.EnumerateArray()); Assert.Contains("10", revisoes[0].GetProperty("antes").GetString());
        await s.Send($"/api/transacoes/{Fase2Scenario.Id(t)}/correcao", data, HttpMethod.Put, HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task EndpointsComunsNaoAlteramConfirmadoENaoHaExclusaoFisica()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c); var t = await s.Realizada(id);
        int tid = Fase2Scenario.Id(t);
        await s.Send($"/api/transacoes/{tid}", new { titulo = "Alterado", valor = 99, data = "2026-08-01", tipo = "Receita", categoria = "Outros", metodoPagamento = "Pix" }, HttpMethod.Put, HttpStatusCode.Conflict);
        await s.Send($"/api/transacoes/{tid}/conta", new { contaId = id }, HttpMethod.Patch, HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Conflict, (await s.Client.DeleteAsync($"/api/transacoes/{tid}")).StatusCode);
        await s.Desconsiderar(t, "", HttpStatusCode.BadRequest);
        var descartado = await s.Desconsiderar(t);
        Assert.Equal("Desconsiderada", descartado.GetProperty("estado").GetString());
        Assert.Equal(100, (await s.Saldo(id)).GetProperty("saldoCalculado").GetDecimal());
        await s.Confirmar(descartado, id, expected: HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task CreditoAntigoPreservaOrigemMesmoDepoisDeEditarMetodo()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c); var t = await s.Read("/api/transacoes/102");
        await s.Confirmar(t, id, expected: HttpStatusCode.BadRequest);
        var editado = await s.Send("/api/transacoes/102", new { titulo = "Crédito editado", valor = 50, data = "2026-08-06", tipo = "Despesa", categoria = "Outros", metodoPagamento = "Pix", numeroParcelas = 1 }, HttpMethod.Put);
        Assert.True(editado.GetProperty("creditoLegado").GetBoolean());
        await s.Confirmar(editado, id, expected: HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task TransferenciaPropriaIdentificadaNaoConfirma()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c); var t = await s.Legado(id, "Transferencia");
        var marcado = await s.Send($"/api/transacoes/{Fase2Scenario.Id(t)}/classificacao", new { transferenciaPropria = true, versao = Fase2Scenario.Version(t) }, HttpMethod.Put);
        await s.Confirmar(marcado, id, expected: HttpStatusCode.BadRequest);
        Assert.Equal(100, (await s.Saldo(id)).GetProperty("saldoCalculado").GetDecimal());
    }
    [Fact]
    public async Task ConfirmacoesSimultaneasNaoDuplicamMovimento()
    {
        var c = await s.Conta(100); int id = Fase2Scenario.Id(c); var t = await s.Legado(id);
        var payload = new { contaId = id, dataEfetivacao = "2026-08-05", versao = Fase2Scenario.Version(t) };
        var responses = await Task.WhenAll(
            s.Client.PostAsJsonAsync($"/api/transacoes/{Fase2Scenario.Id(t)}/confirmacao", payload),
            s.Client.PostAsJsonAsync($"/api/transacoes/{Fase2Scenario.Id(t)}/confirmacao", payload));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        await s.Confirmar(t, id); // Repetir após eventual conflito continua idempotente.
        await using var db = fixture.OpenDb();
        Assert.Equal(1, await db.Transacoes.CountAsync(x => x.Id == Fase2Scenario.Id(t)));
        Assert.Equal(20, (await s.Saldo(id)).GetProperty("saldoCalculado").GetDecimal());
    }
}
