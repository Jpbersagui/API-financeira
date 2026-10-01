using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace ControleFinanceiro.API.Tests;

public class Fase2InterfaceTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    [Fact]
    public async Task AberturaRealizacaoRevisaoCorrecaoDesconsideracaoEExtrato()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
        var page = await browser.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await page.RouteAsync("**/*", async route =>
        {
            var uri = new Uri(route.Request.Url);
            if (uri.Host != "fase2.test") { await route.AbortAsync(); return; }
            using var request = new HttpRequestMessage(new HttpMethod(route.Request.Method), uri.PathAndQuery);
            if (route.Request.PostData != null)
                request.Content = new StringContent(route.Request.PostData, Encoding.UTF8, "application/json");
            using var response = await fixture.Client.SendAsync(request);
            await route.FulfillAsync(new() { Status = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString(), BodyBytes = await response.Content.ReadAsByteArrayAsync() });
        });
        await page.GotoAsync("https://fase2.test/");
        await page.Locator("#conta-nome").FillAsync("Conta da interface");
        await page.Locator("#conta-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#ab-conta option")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("Defina o saldo inicial");
        await Assertions.Expect(page.Locator("#finance-contas")).Not.ToContainTextAsync("0,00");
        await Assertions.Expect(page.Locator("#cadastro-antigo")).Not.ToHaveAttributeAsync("open", "");
        await page.Locator("#finance-contas").GetByRole(AriaRole.Button, new() { Name = "Informar saldo inicial", Exact = true }).ClickAsync();
        await page.Locator("#ab-data").FillAsync("2026-08-01");
        await page.Locator("#ab-valor").FillAsync("850");
        await page.Locator("#form-abertura button").ClickAsync();
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("850,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Recebi dinheiro", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#mov-title")).ToHaveTextAsync("Novo recebimento");
        await page.Locator("#mov-conta").SelectOptionAsync(new SelectOptionValue { Label = "Conta da interface" });
        await page.Locator("#mov-data").FillAsync("2026-08-02");
        await page.Locator("#mov-titulo").FillAsync("Recebimento pela interface");
        await page.Locator("#mov-valor").FillAsync("100");
        await page.Locator("#mov-categoria").FillAsync("Outros");
        await page.Locator("#mov-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("950,00");
        await page.GetByRole(AriaRole.Button, new() { Name = "Paguei uma despesa", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#mov-title")).ToHaveTextAsync("Novo pagamento");
        await page.Locator("#mov-conta").SelectOptionAsync(new SelectOptionValue { Label = "Conta da interface" });
        await page.Locator("#mov-data").FillAsync("2026-08-05");
        await page.Locator("#mov-titulo").FillAsync("Mercado <realizado>");
        await page.Locator("#mov-valor").FillAsync("50");
        await page.Locator("#mov-tipo").SelectOptionAsync("Despesa");
        await page.Locator("#mov-categoria").FillAsync("Alimentação");
        await page.Locator("#mov-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("900,00");

        await page.Locator("#rev-estado").SelectOptionAsync("Confirmada");
        var real = page.Locator(".review-item").Filter(new() { HasText = "Mercado <realizado>" });
        await real.GetByRole(AriaRole.Button, new() { Name = "Corrigir", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#mov-salvar")).ToHaveTextAsync("Salvar correção");
        await page.Locator("#mov-valor").FillAsync("60");
        await page.Locator("#mov-motivo").FillAsync("Conferido no comprovante");
        await page.Locator("#mov-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("890,00");
        await real.GetByRole(AriaRole.Button, new() { Name = "Conferir lançamento" }).ClickAsync();
        await page.Locator("#registro-acoes").GetByRole(AriaRole.Button, new() { Name = "Ver alterações" }).ClickAsync();
        await Assertions.Expect(page.Locator("#registro-revisoes")).ToContainTextAsync("Conferido no comprovante");
        await Assertions.Expect(page.Locator("#registro-revisoes")).ToContainTextAsync("50,00");
        await Assertions.Expect(page.Locator("#registro-revisoes")).ToContainTextAsync("60,00");
        await page.Locator("#registro-dialog").GetByRole(AriaRole.Button, new() { Name = "Fechar", Exact = true }).ClickAsync();
        await page.Locator("#finance-contas").GetByRole(AriaRole.Button, new() { Name = "Ver extrato", Exact = true }).ClickAsync();
        await page.Locator("#ex-inicio").FillAsync("2026-08-01");
        await page.Locator("#ex-fim").FillAsync("2026-08-31");
        await page.Locator("#form-extrato button").ClickAsync();
        await Assertions.Expect(page.Locator("#extrato-resultado")).ToContainTextAsync("Mercado <realizado>");
        await Assertions.Expect(page.Locator("#extrato-resultado")).ToContainTextAsync("890,00");
        await Assertions.Expect(page.Locator("#extrato-resultado")).ToContainTextAsync("05/08/2026");
        await Assertions.Expect(page.Locator("#extrato-resultado")).Not.ToContainTextAsync("Histórico comum");

        var account = page.Locator("#contas-lista li").Filter(new() { HasText = "Conta da interface" });
        await account.GetByRole(AriaRole.Button, new() { Name = "Inativar", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#mov-conta option")).ToHaveCountAsync(1);
        await page.Locator("#rev-estado").SelectOptionAsync("NaoReconciliada");
        await page.Locator("#rev-conta").SelectOptionAsync(new SelectOptionValue { Label = "Conta da interface (inativa)" });
        await Assertions.Expect(page.Locator("#revisao-lista")).ToContainTextAsync("Nenhum lançamento nesta situação");
        await page.Locator("#rev-conta").SelectOptionAsync("");
        var legacy = page.Locator(".review-item").Filter(new() { HasText = "Histórico comum" });
        await page.Locator("#cadastro-antigo > summary").ClickAsync();
        await page.EvaluateAsync("() => { state.mes = 8; state.ano = 2026; return loadDashboard(); }");
        await page.Locator("#transactions-list tr").Filter(new() { HasText = "Histórico comum" }).GetByRole(AriaRole.Button, new() { Name = "Revisar", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#registro-title")).ToContainTextAsync("Histórico comum");
        await Assertions.Expect(page.Locator("#registro-contexto")).ToContainTextAsync("05/08/2026");
        await page.Locator("#registro-acoes").GetByRole(AriaRole.Button, new() { Name = "Editar dados antes de confirmar" }).ClickAsync();
        await page.Locator("#antigo-categoria").FillAsync("Categoria corrigida");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar dados sem confirmar" }).ClickAsync();
        await Assertions.Expect(page.Locator("#registro-feedback")).ToContainTextAsync("continua aguardando revisão");
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("890,00");
        await page.Locator("#registro-acoes").GetByRole(AriaRole.Button, new() { Name = "Confirmar pagamento", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#mov-salvar")).ToHaveTextAsync("Confirmar pagamento");
        await page.Locator("#mov-conta").SelectOptionAsync(new SelectOptionValue { Label = "Conta da interface (inativa)" });
        await Assertions.Expect(page.Locator("#mov-valor")).ToBeDisabledAsync();
        await page.Locator("#mov-salvar").ClickAsync();
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("809,77");
        var credit = page.Locator(".review-item").Filter(new() { HasText = "Compra parcela 1" });
        await Assertions.Expect(credit.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true })).ToHaveCountAsync(0);
        await page.Locator("#rev-estado").SelectOptionAsync("Confirmada");
        await real.GetByRole(AriaRole.Button, new() { Name = "Desconsiderar", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#form-desconsiderar")).ToContainTextAsync("continuará no histórico");
        await page.Locator("#desconsiderar-motivo").FillAsync("Registro duplicado");
        await page.GetByRole(AriaRole.Button, new() { Name = "Desconsiderar lançamento", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#finance-contas")).ToContainTextAsync("869,77");
        await page.Locator("#rev-estado").SelectOptionAsync("Desconsiderada");
        await Assertions.Expect(page.Locator("#revisao-lista")).ToContainTextAsync("Registro duplicado");
        Assert.Empty(errors);
    }
}
