using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControleFinanceiro.API.Services;

namespace ControleFinanceiro.API.Controllers;

[ApiController]
[Route("api/contas")]
public class ContasController(AppDbContext context, MovimentacaoService movimentos, SaldoService saldos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ContaReadDto>>> GetContas()
        => Ok(await context.Contas.AsNoTracking().OrderBy(c => c.Nome).ThenBy(c => c.Id)
            .Select(c => new ContaReadDto { Id = c.Id, Nome = c.Nome, Tipo = c.Tipo, Ativa = c.Ativa,
                DataAbertura = c.DataAbertura, ValorAbertura = c.ValorAbertura, Versao = Convert.ToBase64String(c.Versao) })
            .ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContaReadDto>> GetConta(int id)
    {
        var conta = await context.Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return conta == null ? NotFound(new { mensagem = "Conta não encontrada." }) : Ok(Map(conta));
    }

    [HttpPost]
    public async Task<ActionResult<ContaReadDto>> PostConta(ContaCreateDto dto)
    {
        var conta = new Conta { Nome = dto.Nome.Trim(), Tipo = dto.Tipo!.Value };
        context.Contas.Add(conta);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetConta), new { id = conta.Id }, Map(conta));
    }

    // Inativação e reativação preservam todos os vínculos. Não há endpoint DELETE.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContaReadDto>> PutConta(int id, ContaUpdateDto dto)
    {
        var conta = await context.Contas.FindAsync(id);
        if (conta == null) return NotFound(new { mensagem = "Conta não encontrada." });
        conta.Nome = dto.Nome.Trim();
        conta.Tipo = dto.Tipo!.Value;
        conta.Ativa = dto.Ativa!.Value;
        await context.SaveChangesAsync();
        return Ok(Map(conta));
    }

    [HttpPut("{id:int}/abertura")]
    public async Task<ActionResult<ContaReadDto>> Abertura(int id, AberturaContaDto dto) => Ok(Map(await movimentos.Abertura(id, dto)));

    [HttpGet("{id:int}/saldo")]
    public async Task<ActionResult<SaldoContaDto>> Saldo(int id, [FromQuery] DateOnly? data) => Ok(await saldos.Conta(id, data ?? saldos.Hoje));

    [HttpGet("saldos")]
    public async Task<ActionResult<SaldoConsolidadoDto>> Saldos([FromQuery] DateOnly? data) => Ok(await saldos.Consolidado(data ?? saldos.Hoje));

    [HttpGet("{id:int}/extrato")]
    public async Task<ActionResult<ExtratoContaDto>> Extrato(int id, [FromQuery] DateOnly inicio, [FromQuery] DateOnly fim)
        => Ok(await saldos.Extrato(id, inicio, fim));

    private static ContaReadDto Map(Conta c) => new() { Id = c.Id, Nome = c.Nome, Tipo = c.Tipo, Ativa = c.Ativa,
        DataAbertura = c.DataAbertura, ValorAbertura = c.ValorAbertura, Versao = Convert.ToBase64String(c.Versao) };
}
