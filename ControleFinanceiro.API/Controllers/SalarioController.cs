using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SalarioController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalarioController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────
        // GET: api/salario
        // Retorna o salário vigente.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Retorna o salário ativo (vigente).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(SalarioReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SalarioReadDto>> GetSalarioAtual()
        {
            var salario = await _context.Salarios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Ativo);

            if (salario == null)
                return NotFound(new { mensagem = "Nenhum salário configurado." });

            return Ok(MapToReadDto(salario));
        }

        // ─────────────────────────────────────────────────────────────
        // POST: api/salario
        // Define ou atualiza o salário. Desativa o anterior.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Define ou atualiza o salário. O salário anterior é desativado
        /// e um novo registro ativo é criado.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SalarioReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SalarioReadDto>> PostSalario(
            [FromBody] SalarioCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Desativar o salário anterior, se existir
            var salarioAtual = await _context.Salarios
                .FirstOrDefaultAsync(s => s.Ativo);

            if (salarioAtual != null)
            {
                salarioAtual.Ativo = false;
                salarioAtual.DataFim = DateTime.Now;
            }

            // Criar novo salário ativo
            var novoSalario = new Salario
            {
                Valor = dto.Valor,
                DiaPagamento = dto.DiaPagamento,
                Ativo = true,
                DataInicio = DateTime.Now,
                DataFim = null
            };

            _context.Salarios.Add(novoSalario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSalarioAtual), MapToReadDto(novoSalario));
        }

        // ─────────────────────────────────────────────────────────────
        // GET: api/salario/historico
        // Retorna o histórico de todos os salários.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Retorna o histórico de todos os salários (ativos e inativos).
        /// </summary>
        [HttpGet("historico")]
        [ProducesResponseType(typeof(List<SalarioReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SalarioReadDto>>> GetHistorico()
        {
            var salarios = await _context.Salarios
                .AsNoTracking()
                .OrderByDescending(s => s.DataInicio)
                .Select(s => MapToReadDto(s))
                .ToListAsync();

            return Ok(salarios);
        }

        private static SalarioReadDto MapToReadDto(Salario s)
        {
            return new SalarioReadDto
            {
                Id = s.Id,
                Valor = s.Valor,
                DiaPagamento = s.DiaPagamento,
                Ativo = s.Ativo,
                DataInicio = s.DataInicio,
                DataFim = s.DataFim
            };
        }
    }
}
