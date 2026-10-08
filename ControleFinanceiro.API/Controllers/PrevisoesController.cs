using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Enums;
using ControleFinanceiro.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.API.Controllers;

[ApiController]
[Route("api/previsoes")]
public class PrevisoesController(PrevisaoService previsoes, MovimentacaoService movimentos) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] PrevisaoFiltroDto filtro) => Ok(await previsoes.Listar(filtro));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Consultar(int id) => Ok(await previsoes.Consultar(id));
    [HttpPost]
    public async Task<IActionResult> Criar(PrevisaoCreateDto dto)
    {
        var p = await previsoes.Criar(dto);
        return CreatedAtAction(nameof(Consultar), new { id = p.Id }, await previsoes.Consultar(p.Id));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, PrevisaoUpdateDto dto)
    { await previsoes.Editar(id, dto); return Ok(await previsoes.Consultar(id)); }
    [HttpPut("{id:int}/valor-final")]
    public async Task<IActionResult> ValorFinal(int id, DefinirValorFinalDto dto)
    { await previsoes.DefinirFinal(id, dto); return Ok(await previsoes.Consultar(id)); }
    [HttpPost("{id:int}/realizacoes")]
    public async Task<IActionResult> Realizar(int id, RealizarPrevisaoDto dto)
    {
        await movimentos.RealizarPrevisao(id, dto);
        return Ok(await previsoes.Consultar(id));
    }
    [HttpPut("{id:int}/transacoes/{transacaoId:int}")]
    public async Task<IActionResult> Vincular(int id, int transacaoId, VincularTransacaoDto dto)
    { await previsoes.Vincular(id, transacaoId, dto); return Ok(await previsoes.Consultar(id)); }
    [HttpPost("{id:int}/transacoes/{transacaoId:int}/desvinculacao")]
    public async Task<IActionResult> Desvincular(int id, int transacaoId, DesvincularTransacaoDto dto)
    { await previsoes.Desvincular(id, transacaoId, dto); return Ok(await previsoes.Consultar(id)); }
    [HttpPost("{id:int}/encerramento")]
    public async Task<IActionResult> Encerrar(int id, AlterarEstadoPrevisaoDto dto)
    { await previsoes.AlterarEstado(id, dto, EstadoPrevisao.Encerrada); return Ok(await previsoes.Consultar(id)); }
    [HttpPost("{id:int}/cancelamento")]
    public async Task<IActionResult> Cancelar(int id, AlterarEstadoPrevisaoDto dto)
    { await previsoes.AlterarEstado(id, dto, EstadoPrevisao.Cancelada); return Ok(await previsoes.Consultar(id)); }
    [HttpPost("{id:int}/reabertura")]
    public async Task<IActionResult> Reabrir(int id, AlterarEstadoPrevisaoDto dto)
    { await previsoes.AlterarEstado(id, dto, EstadoPrevisao.Ativa); return Ok(await previsoes.Consultar(id)); }
    [HttpGet("{id:int}/revisoes")]
    public async Task<IActionResult> Historico(int id) => Ok(await previsoes.Historico(id));
}
