using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ControleFinanceiro.API.Enums;
using ControleFinanceiro.API.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControleFinanceiro.API.Tests;

public class Fase1Tests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private HttpClient Client => fixture.Client;

    private async Task<int> Conta(bool ativa = true)
    {
        var response = await Client.PostAsJsonAsync("/api/contas", new { nome = "Conta teste", tipo = "ContaCorrente" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        if (!ativa) Assert.Equal(HttpStatusCode.OK, (await Client.PutAsJsonAsync($"/api/contas/{id}", new { nome = "Conta teste", tipo = "ContaCorrente", ativa })).StatusCode);
        return id;
    }

    private static object Lancamento(int? contaId = null, string metodo = "Pix", int parcelas = 1) => new
    {
        titulo = "Teste", valor = 100.01m, data = "2026-09-23T00:00:00", tipo = "Despesa",
        categoria = "Outros", metodoPagamento = metodo, numeroParcelas = parcelas, contaId
    };

    private async Task<int> Historico(int? contaId = null)
    {
        await using var db = fixture.OpenDb();
        var t = new Transacao { Titulo = "Antes", Valor = 85.19m, Data = new DateTime(2026, 8, 3, 15, 22, 10), Tipo = TipoTransacao.Despesa, Categoria = "Teste", MetodoPagamento = MetodoPagamento.Pix, ContaId = contaId };
        db.Transacoes.Add(t); await db.SaveChangesAsync(); return t.Id;
    }

    [Fact]
    public async Task MigrationPreservaHistoricoSemAssociarOuCriarContas()
    {
        Assert.Equal(fixture.BeforeMigration, fixture.AfterMigration);
        Assert.Equal(0, fixture.AccountsAfterMigration);
        await using var db = fixture.OpenDb();
        var antigas = await db.Transacoes.Where(t => t.Id >= 101 && t.Id <= 103).ToListAsync();
        Assert.Equal(3, antigas.Count);
        Assert.All(antigas, t => Assert.Null(t.ContaId));
        Assert.Equal(102, antigas.Single(t => t.Id == 103).TransacaoOrigemId);
        Assert.Contains((await db.Database.GetAppliedMigrationsAsync()), m => m.EndsWith("_AdicionarContas"));
    }

    [Fact]
    public void InicializacaoNaoGeraSalarioRealizado() => Assert.Equal(3, fixture.StartupTransactionCount);

    [Fact]
    public async Task ContasPodemSerCriadasConsultadasEditadasInativadasEReativadas()
    {
        int id = await Conta();
        foreach (bool ativa in new[] { false, true })
        {
            var update = await Client.PutAsJsonAsync($"/api/contas/{id}", new { nome = "Carteira editada", tipo = "Carteira", ativa });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var read = await Client.GetFromJsonAsync<JsonElement>($"/api/contas/{id}");
            Assert.Equal("Carteira editada", read.GetProperty("nome").GetString());
            Assert.Equal("Carteira", read.GetProperty("tipo").GetString());
            Assert.Equal(ativa, read.GetProperty("ativa").GetBoolean());
        }
        var list = await Client.GetFromJsonAsync<JsonElement>("/api/contas");
        Assert.Contains(list.EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NomeVazioNaoCriaConta(string nome) => Assert.Equal(HttpStatusCode.BadRequest,
        (await Client.PostAsJsonAsync("/api/contas", new { nome, tipo = "Carteira" })).StatusCode);

    [Fact]
    public async Task TipoDeContaInvalidoNaoCriaConta() => Assert.Equal(HttpStatusCode.BadRequest,
        (await Client.PostAsJsonAsync("/api/contas", new { nome = "Teste", tipo = 999 })).StatusCode);

    [Theory]
    [InlineData(null)]
    [InlineData(999999)]
    public async Task NovoLancamentoExigeContaExistente(int? contaId) => Assert.Equal(HttpStatusCode.BadRequest,
        (await Client.PostAsJsonAsync("/api/transacoes", Lancamento(contaId))).StatusCode);

    [Fact]
    public async Task NovoLancamentoNaoAceitaContaInativa() => Assert.Equal(HttpStatusCode.BadRequest,
        (await Client.PostAsJsonAsync("/api/transacoes", Lancamento(await Conta(false)))).StatusCode);

    [Fact]
    public async Task NovoLancamentoAceitaContaAtivaSemConfirmarRealizacao()
    {
        var id = await Conta();
        var response = await Client.PostAsJsonAsync("/api/transacoes", Lancamento(id));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await response.Content.ReadFromJsonAsync<JsonElement>())[0];
        Assert.Equal(id, item.GetProperty("contaId").GetInt32());
        Assert.Equal("Não reconciliado", item.GetProperty("situacaoFinanceira").GetString());
        Assert.Equal("Conta teste", item.GetProperty("contaNome").GetString());
        var savedId = item.GetProperty("id").GetInt32();
        var read = await Client.GetFromJsonAsync<JsonElement>($"/api/transacoes/{savedId}");
        Assert.Equal(id, read.GetProperty("contaId").GetInt32());
        var list = await Client.GetFromJsonAsync<JsonElement>("/api/transacoes");
        Assert.Contains(list.EnumerateArray(), t => t.GetProperty("id").GetInt32() == savedId && t.GetProperty("contaNome").GetString() == "Conta teste");
    }

    [Fact]
    public async Task CreditoContinuaParcelandoSemContaBancaria()
    {
        var response = await Client.PostAsJsonAsync("/api/transacoes", Lancamento(null, "CartaoCredito", 3));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var items = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        Assert.Equal(3, items.Length);
        Assert.Equal(100.01m, items.Sum(i => i.GetProperty("valor").GetDecimal()));
        Assert.All(items, i => { Assert.True(i.GetProperty("semConta").GetBoolean()); Assert.False(i.TryGetProperty("contaId", out _)); });
        Assert.All(items.Skip(1), i => Assert.Equal(items[0].GetProperty("id").GetInt32(), i.GetProperty("transacaoOrigemId").GetInt32()));
    }

    [Fact]
    public async Task CreditoNaoAceitaContaNemAssociacaoDeHistorico()
    {
        int contaId = await Conta();
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/transacoes", Lancamento(contaId, "CartaoCredito"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PatchAsJsonAsync("/api/transacoes/102/conta", new { contaId })).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AssociacaoSomenteAlteraVinculoInclusiveContaInativa(bool ativa)
    {
        int id = await Historico(), contaId = await Conta(ativa);
        await using var db = fixture.OpenDb();
        var before = await db.Transacoes.AsNoTracking().SingleAsync(t => t.Id == id);
        var response = await Client.PatchAsJsonAsync($"/api/transacoes/{id}/conta", new { contaId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Não reconciliado", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("situacaoFinanceira").GetString());
        var after = await db.Transacoes.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(contaId, after.ContaId);
        Assert.Equal((before.Id,before.Titulo,before.Valor,before.Data,before.Categoria,before.Tipo,before.MetodoPagamento,before.NumeroParcelas,before.ParcelaAtual,before.TransacaoOrigemId),
            (after.Id,after.Titulo,after.Valor,after.Data,after.Categoria,after.Tipo,after.MetodoPagamento,after.NumeroParcelas,after.ParcelaAtual,after.TransacaoOrigemId));
    }

    [Fact]
    public async Task AssociacaoRejeitaContaInexistente()
        => Assert.Equal(HttpStatusCode.BadRequest, (await Client.PatchAsJsonAsync($"/api/transacoes/{await Historico()}/conta", new { contaId = 999999 })).StatusCode);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EditarHistoricoPreservaContaInativaOuAusente(bool vinculada)
    {
        int? contaId = vinculada ? await Conta(false) : null;
        int id = await Historico(contaId);
        var response = await Client.PutAsJsonAsync($"/api/transacoes/{id}", Lancamento());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = fixture.OpenDb();
        var t = await db.Transacoes.FindAsync(id);
        Assert.Equal(contaId, t!.ContaId);
        Assert.Equal("Teste", t.Titulo);
    }

    [Fact]
    public async Task ContaComHistoricoNaoPodeSerExcluidaPorApiOuCascata()
    {
        int id = await Conta(); await Historico(id);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Client.DeleteAsync($"/api/contas/{id}")).StatusCode);
        await using var db = fixture.OpenDb();
        db.Contas.Remove((await db.Contas.FindAsync(id))!);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(await db.Transacoes.AnyAsync(t => t.ContaId == id));
    }

    [Fact]
    public async Task ResumosDeclaramSemanticaLegadaEIncluemContas()
    {
        int contaId = await Conta(); await Historico(contaId);
        foreach (var path in new[] { "/api/dashboard?mes=8&ano=2026", "/api/transacoes/balanco-mensal?mes=8&ano=2026" })
        {
            var json = await Client.GetFromJsonAsync<JsonElement>(path);
            Assert.False(json.GetProperty("saldoBancarioConfirmado").GetBoolean());
            Assert.Contains("não representam saldo bancário confirmado", json.GetProperty("aviso").GetString());
            if (path.Contains("dashboard")) Assert.Contains(json.GetProperty("transacoesDoMes").EnumerateArray(),
                t => t.TryGetProperty("contaId", out var id) && id.GetInt32() == contaId && t.GetProperty("contaNome").GetString() == "Conta teste");
        }
        var html = await Client.GetStringAsync("/");
        Assert.Contains("Resultado dos lançamentos", html);
        Assert.DoesNotContain("Saldo da Conta", html);
        Assert.Contains("associar", html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("configuracao invalida")]
    [InlineData("Server=localhost")]
    public async Task SemConnectionStringValidaAplicacaoFalhaSemAbrirBanco(string connectionString)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        start.ArgumentList.Add("--ConnectionStrings:DefaultConnection=" + connectionString);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try { await process.WaitForExitAsync(timeout.Token); }
        finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        Assert.NotEqual(0, process.ExitCode);
        Assert.Contains("Configure ConnectionStrings:DefaultConnection", await stdout + await stderr);
    }
}
