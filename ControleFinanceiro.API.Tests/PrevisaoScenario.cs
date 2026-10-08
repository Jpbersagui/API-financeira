using System.Net;
using System.Text.Json;
using static ControleFinanceiro.API.Tests.Fase2Scenario;
namespace ControleFinanceiro.API.Tests;
public class PrevisaoScenario(SqlFixture fixture) : Fase2Scenario(fixture)
{
    public Task<JsonElement> Previsao(decimal valor = 3000, string tipo = "Receita", string data = "2026-08-05", int? contaId = null)
        => Send("/api/previsoes", new { descricao = "Previsão de teste", valorPrevistoOriginal = valor, tipo,
            dataPrevista = data, categoria = "Outros", contaId }, expected: HttpStatusCode.Created);
    public Task<JsonElement> Realizar(JsonElement p, int contaId, decimal valor, string data = "2026-08-05", HttpStatusCode expected = HttpStatusCode.OK)
        => Send($"/api/previsoes/{Id(p)}/realizacoes", new { titulo = "Realização", tipo = p.GetProperty("tipo").GetString(), valor,
            dataEfetivacao = data, contaId, categoria = "Outros", metodoPagamento = "Pix", versao = Version(p) }, expected: expected);
    public Task<JsonElement> Final(JsonElement p, decimal valor, HttpStatusCode expected = HttpStatusCode.OK)
        => Send($"/api/previsoes/{Id(p)}/valor-final", new { valorFinal = valor, versao = Version(p) }, HttpMethod.Put, expected);
    public Task<JsonElement> Estado(JsonElement p, string acao, string motivo = "Conferido", HttpStatusCode expected = HttpStatusCode.OK)
        => Send($"/api/previsoes/{Id(p)}/{acao}", new { motivo, versao = Version(p) }, expected: expected);
    public Task<JsonElement> Vincular(JsonElement p, JsonElement t, HttpStatusCode expected = HttpStatusCode.OK)
        => Send($"/api/previsoes/{Id(p)}/transacoes/{Id(t)}", new { versao = Version(p), versaoTransacao = Version(t) }, HttpMethod.Put, expected);
    public Task<JsonElement> Desvincular(JsonElement p, JsonElement t, HttpStatusCode expected = HttpStatusCode.OK)
        => Send($"/api/previsoes/{Id(p)}/transacoes/{Id(t)}/desvinculacao", new { motivo = "Vínculo incorreto", versao = Version(p), versaoTransacao = Version(t) }, expected: expected);
}
