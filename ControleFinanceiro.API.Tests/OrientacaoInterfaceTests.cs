using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace ControleFinanceiro.API.Tests;

public class OrientacaoInterfaceTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    [Fact]
    public async Task OrientaSaldoInicialPreservaFormularioEExplicaCreditoEmTelaPequena()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
        var page = await browser.NewPageAsync(new() { Locale = "pt-BR", ViewportSize = new() { Width = 390, Height = 844 } });
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await page.RouteAsync("**/*", async route =>
        {
            var uri = new Uri(route.Request.Url);
            if (uri.Host != "orientacao.test") { await route.AbortAsync(); return; }
            using var request = new HttpRequestMessage(new HttpMethod(route.Request.Method), uri.PathAndQuery);
            if (route.Request.PostData != null) request.Content = new StringContent(route.Request.PostData, Encoding.UTF8, "application/json");
            using var response = await fixture.Client.SendAsync(request);
            await route.FulfillAsync(new() { Status = (int)response.StatusCode, ContentType = response.Content.Headers.ContentType?.ToString(), BodyBytes = await response.Content.ReadAsByteArrayAsync() });
        });
        await page.GotoAsync("https://orientacao.test/");
        await page.GetByRole(AriaRole.Button, new() { Name = "Cadastre sua primeira conta" }).ClickAsync();
        await page.Locator("#conta-nome").FillAsync("Carteira pessoal");
        await page.Locator("#conta-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#finance-resumo")).ToContainTextAsync("parcial");
        await page.GetByRole(AriaRole.Button, new() { Name = "Paguei uma despesa", Exact = true }).ClickAsync();
        await page.Locator("#mov-conta").SelectOptionAsync(new SelectOptionValue { Label = "Carteira pessoal" });
        await page.Locator("#mov-data").FillAsync("2026-08-05");
        await page.Locator("#mov-titulo").FillAsync("Compra preservada");
        await page.Locator("#mov-valor").FillAsync("20");
        await page.Locator("#mov-categoria").FillAsync("Outros");
        await Assertions.Expect(page.Locator("#mov-conta-ajuda")).ToContainTextAsync("Informe o saldo inicial");
        await page.Locator("#mov-definir-abertura").ClickAsync();
        await page.Locator("#ab-data").FillAsync("2026-08-01");
        await page.Locator("#ab-valor").FillAsync("100");
        await page.Locator("#ab-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#ab-feedback")).ToContainTextAsync("Saldo inicial salvo");
        await Assertions.Expect(page.Locator("#mov-titulo")).ToHaveValueAsync("Compra preservada");
        await Assertions.Expect(page.Locator("#mov-valor")).ToHaveValueAsync("20");
        await page.Locator("#mov-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("80,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Revisar lançamentos", Exact = true }).First.ClickAsync();
        var credit = page.Locator(".review-item").Filter(new() { HasText = "Compra parcela 1" });
        await credit.GetByRole(AriaRole.Button, new() { Name = "Conferir lançamento" }).ClickAsync();
        await Assertions.Expect(page.Locator("#registro-aviso")).ToContainTextAsync("não representa uma saída da conta");
        await Assertions.Expect(page.Locator("#registro-acoes").GetByRole(AriaRole.Button, new() { Name = "Confirmar pagamento", Exact = true })).ToHaveCountAsync(0);
        await page.Locator("#registro-acoes").GetByRole(AriaRole.Button, new() { Name = "Desconsiderar", Exact = true }).ClickAsync();
        await page.Locator("#form-desconsiderar").GetByRole(AriaRole.Button, new() { Name = "Cancelar", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#form-desconsiderar")).ToBeHiddenAsync();
        await page.Locator("#registro-dialog").GetByRole(AriaRole.Button, new() { Name = "Fechar", Exact = true }).ClickAsync();
        await Assertions.Expect(credit).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= window.innerWidth"));
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.ScreenshotAsync(new() { Path = Path.Combine(Path.GetTempPath(), "financeiro-ux-mobile.png") });
        await page.SetViewportSizeAsync(1280, 900);
        await page.ScreenshotAsync(new() { Path = Path.Combine(Path.GetTempPath(), "financeiro-ux-desktop.png") });
        Assert.Empty(errors);
    }
}
