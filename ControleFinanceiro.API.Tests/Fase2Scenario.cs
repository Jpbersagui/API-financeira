using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ControleFinanceiro.API.Tests;

public class Fase2Scenario(SqlFixture fixture)
{
    public HttpClient Client => fixture.Client;
    public static int Id(JsonElement json) => json.GetProperty("id").GetInt32();
    public static string Version(JsonElement json) => json.GetProperty("versao").GetString()!;
    public async Task<JsonElement> Read(string path) => await Client.GetFromJsonAsync<JsonElement>(path);
    public async Task<JsonElement> Send(string path, object data, HttpMethod? method = null, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var message = new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(data) };
        var response = await Client.SendAsync(message);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{path}: expected {expected}, got {response.StatusCode}: {text}");
        return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }
    public async Task<JsonElement> Conta(decimal? abertura = null, string data = "2026-08-01")
    {
        var conta = await Send("/api/contas", new { nome = "Fase 2", tipo = "ContaCorrente" }, expected: HttpStatusCode.Created);
        if (abertura.HasValue) conta = await Abrir(conta, abertura.Value, data);
        return conta;
    }
    public Task<JsonElement> Abrir(JsonElement conta, decimal valor, string data, HttpStatusCode expected = HttpStatusCode.OK)
        => Send($"/api/contas/{Id(conta)}/abertura", new { dataAbertura = data, valorAbertura = valor, versao = Version(conta) }, HttpMethod.Put, expected);
    public Task<JsonElement> Realizada(int contaId, decimal valor = 80, string tipo = "Despesa", string data = "2026-08-05", HttpStatusCode expected = HttpStatusCode.Created)
        => Send("/api/transacoes/realizadas", new { titulo = "Realizado", valor, tipo, dataEfetivacao = data, contaId, categoria = "Outros", metodoPagamento = "Pix" }, expected: expected);
    public async Task<JsonElement> Legado(int contaId, string metodo = "Pix")
    {
        var list = await Send("/api/transacoes", new { titulo = "Legado", valor = 80, tipo = "Despesa", data = "2026-08-05T13:14:15", contaId = metodo == "CartaoCredito" ? (int?)null : contaId, categoria = "Outros", metodoPagamento = metodo }, expected: HttpStatusCode.Created);
        return list[0];
    }
    public Task<JsonElement> Confirmar(JsonElement t, int contaId, string data = "2026-08-05", HttpStatusCode expected = HttpStatusCode.OK)
        => Send($"/api/transacoes/{Id(t)}/confirmacao", new { contaId, dataEfetivacao = data, versao = Version(t) }, expected: expected);
    public Task<JsonElement> Desconsiderar(JsonElement t, string motivo = "Registro incorreto", HttpStatusCode expected = HttpStatusCode.OK)
        => Send($"/api/transacoes/{Id(t)}/desconsideracao", new { motivo, versao = Version(t) }, expected: expected);
    public Task<JsonElement> Saldo(int contaId, string data = "2026-08-31") => Read($"/api/contas/{contaId}/saldo?data={data}");
}
