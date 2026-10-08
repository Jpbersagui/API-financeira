using System.Text;
using Microsoft.Playwright;
using Xunit;
using static ControleFinanceiro.API.Tests.Fase2Scenario;
namespace ControleFinanceiro.API.Tests;
public class Fase3InterfaceTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    [Theory]
    [InlineData(1280)]
    [InlineData(390)]
    public async Task PreverRealizarVincularEncerrarReabrirEConsultarHistorico(int largura)
    {
        var s = new PrevisaoScenario(fixture); var c = await s.Conta(1000);
        var t = await s.Realizada(Id(c), 50, "Receita");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = largura, Height = 844 } });
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.RouteAsync("**/*", async route =>
        {
            var uri = new Uri(route.Request.Url);
            if (uri.Host != "fase3.test") { await route.AbortAsync(); return; }
            using var request = new HttpRequestMessage(new HttpMethod(route.Request.Method), uri.PathAndQuery);
            if (route.Request.PostData != null) request.Content = new StringContent(route.Request.PostData, Encoding.UTF8, "application/json");
            using var response = await fixture.Client.SendAsync(request);
            await route.FulfillAsync(new() { Status = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString(), BodyBytes = await response.Content.ReadAsByteArrayAsync() });
        });
        await page.GotoAsync("https://fase3.test/");
        await page.GetByRole(AriaRole.Button, new() { Name = "Espero receber", Exact = true }).ClickAsync();
        await page.Locator("#prev-descricao").FillAsync("Salário <planejado> " + largura);
        await page.Locator("#prev-original").FillAsync("300");
        await page.Locator("#prev-data").FillAsync("2026-08-05");
        await page.Locator("#prev-categoria").FillAsync("Salário");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar previsão", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#prev-contexto")).ToContainTextAsync("05/08/2026");
        await Assertions.Expect(page.Locator("#prev-valores")).ToContainTextAsync("Restante: R$ 300,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Registrar realização parcial", Exact = true }).ClickAsync();
        await page.Locator("#mov-conta").SelectOptionAsync(Id(c).ToString());
        await page.Locator("#mov-data").FillAsync("2026-08-05");
        await page.Locator("#mov-valor").FillAsync("100");
        await page.Locator("#mov-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#prev-valores")).ToContainTextAsync("Restante: R$ 200,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Vincular movimentação já registrada", Exact = true }).ClickAsync();
        await page.Locator("#prev-transacao").SelectOptionAsync(Id(t).ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Vincular movimentação", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#prev-valores")).ToContainTextAsync("Realizado: R$ 150,00");
        var vinculado = page.Locator("#prev-movimentos .review-item").Filter(new() { HasText = "Realizado" });
        await vinculado.GetByRole(AriaRole.Button, new() { Name = "Remover vínculo", Exact = true }).ClickAsync();
        await page.Locator("#prev-motivo").FillAsync("Era outro recebimento");
        await page.Locator("#prev-confirmar-acao").ClickAsync();
        await Assertions.Expect(page.Locator("#prev-valores")).ToContainTextAsync("Realizado: R$ 100,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Informar valor final", Exact = true }).ClickAsync();
        await page.Locator("#prev-final").FillAsync("250");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar valor final", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#prev-valores")).ToContainTextAsync("Final: R$ 250,00");
        await Assertions.Expect(page.Locator("#prev-valores")).ToContainTextAsync("Original: R$ 300,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Encerrar restante", Exact = true }).ClickAsync();
        await page.Locator("#prev-motivo").FillAsync("Acordo final");
        await page.Locator("#prev-confirmar-acao").ClickAsync();
        await Assertions.Expect(page.Locator("#prev-decisao")).ToContainTextAsync("Encerrada");
        await Assertions.Expect(page.Locator("#prev-valores")).ToContainTextAsync("Restante: R$ 0,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Reabrir previsão", Exact = true }).ClickAsync();
        await page.Locator("#prev-motivo").FillAsync("Revisão do acordo");
        await page.Locator("#prev-confirmar-acao").ClickAsync();
        await Assertions.Expect(page.Locator("#prev-valores")).ToContainTextAsync("Restante: R$ 150,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Registrar realização total", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#mov-valor")).ToHaveValueAsync("150");
        await page.Locator("#mov-conta").SelectOptionAsync(Id(c).ToString());
        await page.Locator("#mov-data").FillAsync("2026-08-06");
        await page.Locator("#mov-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#prev-decisao")).ToContainTextAsync("Valor realizado");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ver histórico da previsão", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#prev-historico")).ToContainTextAsync("Revisão do acordo");
        await Assertions.Expect(page.Locator("#prev-historico")).ToContainTextAsync("Era outro recebimento");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fechar previsão", Exact = true }).ClickAsync();
        Assert.Equal(1300, (await s.Saldo(Id(c))).GetProperty("saldoCalculado").GetDecimal());

        await page.GetByRole(AriaRole.Button, new() { Name = "Espero pagar", Exact = true }).ClickAsync();
        await page.Locator("#prev-descricao").FillAsync("Compra desistida");
        await page.Locator("#prev-original").FillAsync("20"); await page.Locator("#prev-categoria").FillAsync("Outros");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar previsão", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancelar previsão", Exact = true }).ClickAsync();
        await page.Locator("#prev-motivo").FillAsync("Não será necessária");
        await page.Locator("#prev-confirmar-acao").ClickAsync();
        await Assertions.Expect(page.Locator("#prev-decisao")).ToContainTextAsync("Cancelada");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fechar previsão", Exact = true }).ClickAsync();
        Assert.False(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > innerWidth"));
        Assert.Empty(errors);
    }
}
