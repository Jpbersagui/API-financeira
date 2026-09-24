using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Controllers;

[ApiController]
[Route("api/contas")]
public class ContasController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ContaReadDto>>> GetContas()
        => Ok(await context.Contas.AsNoTracking().OrderBy(c => c.Nome).ThenBy(c => c.Id)
            .Select(c => new ContaReadDto { Id = c.Id, Nome = c.Nome, Tipo = c.Tipo, Ativa = c.Ativa })
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

    private static ContaReadDto Map(Conta c) => new() { Id = c.Id, Nome = c.Nome, Tipo = c.Tipo, Ativa = c.Ativa };
}
