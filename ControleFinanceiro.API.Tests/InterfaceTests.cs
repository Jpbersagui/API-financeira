using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace ControleFinanceiro.API.Tests;

// Navegador usa a API em memória de hospedagem, conectada ao SQL Server de teste.
// Nenhum servidor da aplicação pessoal é iniciado.
public class InterfaceTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    [Fact]
    public async Task UsuarioOrganizaContasELancamentosPelaInterface()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
        var page = await browser.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await page.RouteAsync("**/*", async route =>
        {
            var uri = new Uri(route.Request.Url);
            if (uri.Host != "fase1.test") { await route.AbortAsync(); return; }
            using var request = new HttpRequestMessage(new HttpMethod(route.Request.Method), uri.PathAndQuery);
            if (route.Request.PostData != null)
                request.Content = new StringContent(route.Request.PostData, Encoding.UTF8, "application/json");
            using var response = await fixture.Client.SendAsync(request);
            await route.FulfillAsync(new()
            {
                Status = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString(),
                BodyBytes = await response.Content.ReadAsByteArrayAsync()
            });
        });

        await page.GotoAsync("https://fase1.test/");
        await Assertions.Expect(page.Locator("#resumo-aviso")).ToContainTextAsync("não representam saldo bancário confirmado");
        await page.Locator("#conta-nome").FillAsync("Minha carteira <teste>");
        await page.Locator("#conta-tipo").SelectOptionAsync("Carteira");
        await page.Locator("#conta-salvar").ClickAsync();
        var account = page.Locator("#contas-lista li").Filter(new() { HasText = "Minha carteira <teste>" });
        await Assertions.Expect(account).ToContainTextAsync("Ativa");
        await account.GetByRole(AriaRole.Button, new() { Name = "Editar", Exact = true }).ClickAsync();
        await page.Locator("#conta-nome").FillAsync("Carteira editada <teste>");
        await page.Locator("#conta-salvar").ClickAsync();
        account = page.Locator("#contas-lista li").Filter(new() { HasText = "Carteira editada <teste>" });
        await Assertions.Expect(account).ToBeVisibleAsync();
        await account.GetByRole(AriaRole.Button, new() { Name = "Inativar", Exact = true }).ClickAsync();
        await Assertions.Expect(account).ToContainTextAsync("Inativa");
        await Assertions.Expect(page.Locator("#tx-conta option")).ToHaveCountAsync(1);

        // Associação de registro histórico à conta inativa pelo mês correspondente.
        await page.Locator("#cadastro-antigo > summary").ClickAsync();
        await page.EvaluateAsync("() => { state.mes = 8; state.ano = 2026; return loadDashboard(); }");
        var legacy = page.Locator("#transactions-list tr").Filter(new() { HasText = "Histórico comum" });
        await legacy.Locator("select").SelectOptionAsync(new SelectOptionValue { Label = "Carteira editada <teste> (inativa)" });
        await legacy.GetByRole(AriaRole.Button, new() { Name = "Informar conta" }).ClickAsync();
        await Assertions.Expect(legacy.Locator("td").Nth(4).Locator("span")).ToHaveTextAsync("Carteira editada <teste>");
        await Assertions.Expect(legacy).ToContainTextAsync("Aguardando revisão");

        await account.GetByRole(AriaRole.Button, new() { Name = "Reativar", Exact = true }).ClickAsync();
        await Assertions.Expect(account).ToContainTextAsync("Ativa");
        await page.Locator("#tx-titulo").FillAsync("Almoço teste");
        await page.Locator("#tx-valor").FillAsync("25.50");
        await page.Locator("#tx-data").FillAsync("2026-08-10");
        await page.Locator("#tx-categoria").SelectOptionAsync("Alimentação");
        await page.Locator("#tx-conta").SelectOptionAsync(new SelectOptionValue { Label = "Carteira editada <teste>" });
        await page.Locator("#btn-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#transactions-list")).ToContainTextAsync("Almoço teste");

        await page.Locator("#tx-metodo").SelectOptionAsync("CartaoCredito");
        await Assertions.Expect(page.Locator("#tx-conta")).ToBeDisabledAsync();
        await page.Locator("#tx-titulo").FillAsync("Crédito teste");
        await page.Locator("#tx-valor").FillAsync("90");
        await page.Locator("#tx-data").FillAsync("2026-08-11");
        await page.Locator("#tx-categoria").SelectOptionAsync("Tecnologia");
        await page.Locator("#tx-parcelas").FillAsync("3");
        await page.Locator("#btn-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#transactions-list")).ToContainTextAsync("Crédito teste");
        await Assertions.Expect(page.Locator("#tx-conta")).ToBeEnabledAsync();
        Assert.Empty(errors);
    }
}
