using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Enums;
using ControleFinanceiro.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControleFinanceiro.API.Services;

namespace ControleFinanceiro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TransacoesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly MovimentacaoService _movimentos;
        private readonly SaldoService _saldos;

        public TransacoesController(AppDbContext context, MovimentacaoService movimentos, SaldoService saldos)
        {
            _context = context;
            _movimentos = movimentos;
            _saldos = saldos;
        }

        // ─────────────────────────────────────────────────────────────
        // GET: api/transacoes
        // Lista todas as transações com filtro opcional por método de pagamento.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Lista todas as transações. Permite filtrar por método de pagamento.
        /// </summary>
        /// <param name="metodoPagamento">Filtro opcional: Pix, CartaoCredito, Debito, Dinheiro, Transferencia</param>
        [HttpGet]
        [ProducesResponseType(typeof(List<TransacaoReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TransacaoReadDto>>> GetTransacoes(
            [FromQuery] MetodoPagamento? metodoPagamento, [FromQuery] EstadoTransacao? estado,
            [FromQuery] int? contaId, [FromQuery] ClassificacaoPendenteTransacao? classificacao)
        {
            IQueryable<Transacao> query = _context.Transacoes.Include(t => t.Conta).AsNoTracking();
            if (estado.HasValue) query = query.Where(t => t.Estado == estado);
            if (contaId.HasValue) query = query.Where(t => t.ContaId == contaId);
            if (classificacao.HasValue) query = query.Where(t => t.ClassificacaoPendente == classificacao);

            if (metodoPagamento.HasValue)
            {
                query = query.Where(t => t.MetodoPagamento == metodoPagamento.Value);
            }

            var transacoes = await query
                .OrderByDescending(t => t.Data)
                .Select(t => MapToReadDto(t))
                .ToListAsync();

            return Ok(transacoes);
        }

        // ─────────────────────────────────────────────────────────────
        // GET: api/transacoes/{id}
        // Retorna uma transação específica por ID.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Retorna uma transação por ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(TransacaoReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TransacaoReadDto>> GetTransacao(int id)
        {
            var transacao = await _context.Transacoes
                .Include(t => t.Conta)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transacao == null)
                return NotFound(new { mensagem = $"Transação com ID {id} não encontrada." });

            return Ok(MapToReadDto(transacao));
        }

        // ─────────────────────────────────────────────────────────────
        // POST: api/transacoes
        // Cria uma nova transação. Se for cartão de crédito parcelado,
        // gera automaticamente os lançamentos futuros.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Cria uma nova transação financeira. Se o método de pagamento for Cartão de Crédito
        /// e o número de parcelas for maior que 1, gera automaticamente os lançamentos parcelados.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(List<TransacaoReadDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TransacaoReadDto>>> PostTransacao(
            [FromBody] TransacaoCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validação: parcelamento só é permitido com Cartão de Crédito
            if (dto.NumeroParcelas > 1 && dto.MetodoPagamento != MetodoPagamento.CartaoCredito)
            {
                return BadRequest(new { mensagem = "Parcelamento só é permitido com o método de pagamento Cartão de Crédito." });
            }

            Conta? conta = null;
            if (dto.MetodoPagamento == MetodoPagamento.CartaoCredito)
            {
                if (dto.ContaId != null)
                    return BadRequest(new { mensagem = "Compras no crédito legado devem permanecer sem conta bancária." });
            }
            else
            {
                conta = await _context.Contas.FirstOrDefaultAsync(c => c.Id == dto.ContaId && c.Ativa);
                if (conta == null)
                    return BadRequest(new { mensagem = "Selecione uma conta existente e ativa para este lançamento." });
            }

            var transacoesCriadas = new List<Transacao>();

            // ── Lógica de Parcelamento ──
            if (dto.MetodoPagamento == MetodoPagamento.CartaoCredito && dto.NumeroParcelas > 1)
            {
                // Preservar o parcelamento legado como uma única gravação lógica.
                await using var parcelamento = await _context.Database.BeginTransactionAsync();
                decimal valorParcela = Math.Round(dto.Valor / dto.NumeroParcelas, 2);
                // Ajustar a diferença de arredondamento na primeira parcela
                decimal diferenca = dto.Valor - (valorParcela * dto.NumeroParcelas);

                // Criar a primeira parcela (transação original)
                var primeiraTransacao = new Transacao
                {
                    OrigemRegistro = OrigemRegistroTransacao.CreditoLegado,
                    Titulo = $"{dto.Titulo} (Parcela 1/{dto.NumeroParcelas})",
                    Valor = valorParcela + diferenca,
                    Data = dto.Data,
                    Tipo = dto.Tipo,
                    Categoria = dto.Categoria,
                    MetodoPagamento = dto.MetodoPagamento,
                    NumeroParcelas = dto.NumeroParcelas,
                    ParcelaAtual = 1,
                    TransacaoOrigemId = null
                };

                _context.Transacoes.Add(primeiraTransacao);
                await _context.SaveChangesAsync(); // Salvar para obter o Id gerado

                transacoesCriadas.Add(primeiraTransacao);

                // Criar as parcelas subsequentes (2..N)
                for (int i = 2; i <= dto.NumeroParcelas; i++)
                {
                    var parcela = new Transacao
                    {
                        OrigemRegistro = OrigemRegistroTransacao.CreditoLegado,
                        Titulo = $"{dto.Titulo} (Parcela {i}/{dto.NumeroParcelas})",
                        Valor = valorParcela,
                        Data = dto.Data.AddMonths(i - 1),
                        Tipo = dto.Tipo,
                        Categoria = dto.Categoria,
                        MetodoPagamento = dto.MetodoPagamento,
                        NumeroParcelas = dto.NumeroParcelas,
                        ParcelaAtual = i,
                        TransacaoOrigemId = primeiraTransacao.Id
                    };

                    _context.Transacoes.Add(parcela);
                    transacoesCriadas.Add(parcela);
                }

                await _context.SaveChangesAsync();
                await parcelamento.CommitAsync();
            }
            else
            {
                // ── Transação à vista (única) ──
                var transacao = new Transacao
                {
                    OrigemRegistro = dto.MetodoPagamento == MetodoPagamento.CartaoCredito ? OrigemRegistroTransacao.CreditoLegado : OrigemRegistroTransacao.LegadoComum,
                    ContaId = dto.ContaId,
                    Conta = conta,
                    Titulo = dto.Titulo,
                    Valor = dto.Valor,
                    Data = dto.Data,
                    Tipo = dto.Tipo,
                    Categoria = dto.Categoria,
                    MetodoPagamento = dto.MetodoPagamento,
                    NumeroParcelas = dto.NumeroParcelas,
                    ParcelaAtual = null,
                    TransacaoOrigemId = null
                };

                _context.Transacoes.Add(transacao);
                await _context.SaveChangesAsync();
                transacoesCriadas.Add(transacao);
            }

            var resultado = transacoesCriadas.Select(t => MapToReadDto(t)).ToList();
            return CreatedAtAction(nameof(GetTransacao), new { id = resultado.First().Id }, resultado);
        }

        // ─────────────────────────────────────────────────────────────
        // PUT: api/transacoes/{id}
        // Atualiza uma transação existente.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Atualiza uma transação existente.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(TransacaoReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TransacaoReadDto>> PutTransacao(
            int id, [FromBody] TransacaoUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var transacao = await _context.Transacoes.FindAsync(id);

            if (transacao == null)
                return NotFound(new { mensagem = $"Transação com ID {id} não encontrada." });

            // A conta só muda pela operação de associação; preservar vínculos existentes.
            if (transacao.Estado != EstadoTransacao.NaoReconciliada)
                return Conflict(new { mensagem = "Use o fluxo específico de correção para movimentos confirmados. Desconsiderados são preservados." });
            if (await _movimentos.Credito(transacao) || dto.MetodoPagamento == MetodoPagamento.CartaoCredito || dto.NumeroParcelas > 1)
                transacao.OrigemRegistro = OrigemRegistroTransacao.CreditoLegado;
            if (dto.MetodoPagamento == MetodoPagamento.CartaoCredito && transacao.ContaId != null)
                return BadRequest(new { mensagem = "Um lançamento associado a conta não pode ser convertido em crédito legado." });

            await _context.Entry(transacao).Reference(t => t.Conta).LoadAsync();
            transacao.Titulo = dto.Titulo;
            transacao.Valor = dto.Valor;
            transacao.Data = dto.Data;
            transacao.Tipo = dto.Tipo;
            transacao.Categoria = dto.Categoria;
            transacao.MetodoPagamento = dto.MetodoPagamento;
            transacao.NumeroParcelas = dto.NumeroParcelas;

            await _context.SaveChangesAsync();

            return Ok(MapToReadDto(transacao));
        }

        // ─────────────────────────────────────────────────────────────
        // PATCH: api/transacoes/{id}/conta
        // Organiza o histórico sem confirmar realização financeira.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Altera somente a conta do lançamento, inclusive para uma conta inativa.
        /// </summary>
        [HttpPatch("{id:int}/conta")]
        public async Task<ActionResult<TransacaoReadDto>> AssociarConta(int id, AssociarContaDto dto)
        {
            var transacao = await _context.Transacoes.FindAsync(id);
            if (transacao == null) return NotFound(new { mensagem = "Lançamento não encontrado." });
            if (transacao.Estado != EstadoTransacao.NaoReconciliada)
                return Conflict(new { mensagem = "A associação comum só altera lançamentos não reconciliados. Use correção para confirmados." });
            if (await _movimentos.Credito(transacao))
                return BadRequest(new { mensagem = "O crédito legado será associado a cartões em uma fase posterior." });
            var conta = await _context.Contas.FindAsync(dto.ContaId);
            if (conta == null) return BadRequest(new { mensagem = "Conta não encontrada." });
            // Conta inativa pode representar histórico; alterar exclusivamente o vínculo.
            transacao.Conta = conta;
            await _context.SaveChangesAsync();
            return Ok(MapToReadDto(transacao));
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTransacao(int id)
        {
            if (!await _context.Transacoes.AnyAsync(t => t.Id == id)) return NotFound();
            return Conflict(new { mensagem = "Use a ação Desconsiderar e informe um motivo. O histórico não será excluído." });
        }

        // ─────────────────────────────────────────────────────────────
        // GET: api/transacoes/balanco-mensal?mes=8&ano=2026
        // Retorna o balanço consolidado de um mês específico.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Retorna o balanço financeiro consolidado de um mês e ano específicos,
        /// incluindo totais de receitas, despesas, saldo e gastos por categoria.
        /// </summary>
        /// <param name="mes">Mês (1 a 12)</param>
        /// <param name="ano">Ano (ex: 2026)</param>
        [HttpGet("balanco-mensal")]
        [ProducesResponseType(typeof(BalancoMensalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BalancoMensalDto>> GetBalancoMensal(
            [FromQuery] int mes, [FromQuery] int ano)
        {
            if (mes < 1 || mes > 12)
                return BadRequest(new { mensagem = "O mês deve ser entre 1 e 12." });

            if (ano < 1900 || ano > 2100)
                return BadRequest(new { mensagem = "O ano deve ser entre 1900 e 2100." });

            var transacoesDoMes = await _context.Transacoes
                .Include(t => t.Conta)
                .AsNoTracking()
                .Where(t => t.Data.Month == mes && t.Data.Year == ano)
                .ToListAsync();

            decimal totalReceitas = transacoesDoMes
                .Where(t => t.Tipo == TipoTransacao.Receita)
                .Sum(t => t.Valor);

            decimal totalDespesas = transacoesDoMes
                .Where(t => t.Tipo == TipoTransacao.Despesa)
                .Sum(t => t.Valor);

            var gastosPorCategoria = transacoesDoMes
                .Where(t => t.Tipo == TipoTransacao.Despesa)
                .GroupBy(t => t.Categoria)
                .Select(g => new GastoPorCategoriaDto
                {
                    Categoria = g.Key,
                    Total = g.Sum(t => t.Valor)
                })
                .OrderByDescending(g => g.Total)
                .ToList();

            var balanco = new BalancoMensalDto
            {
                Mes = mes,
                Ano = ano,
                TotalReceitas = totalReceitas,
                TotalDespesas = totalDespesas,
                SaldoFinal = totalReceitas - totalDespesas,
                Financeiro = await _saldos.Resumo(mes, ano),
                GastosPorCategoria = gastosPorCategoria
            };

            return Ok(balanco);
        }

        // ─────────────────────────────────────────────────────────────
        // Helper: mapeamento Model → ReadDTO
        // ─────────────────────────────────────────────────────────────

        [HttpPost("realizadas")]
        public async Task<ActionResult<TransacaoReadDto>> Realizada(TransacaoRealizadaCreateDto dto)
        {
            var t = await _movimentos.Criar(dto);
            return CreatedAtAction(nameof(GetTransacao), new { id = t.Id }, MapToReadDto(t));
        }
        [HttpPost("{id:int}/confirmacao")]
        public async Task<ActionResult<TransacaoReadDto>> Confirmacao(int id, ConfirmarTransacaoDto dto)
            => Ok(MapToReadDto(await _movimentos.Confirmar(id, dto)));
        [HttpPut("{id:int}/correcao")]
        public async Task<ActionResult<TransacaoReadDto>> Correcao(int id, CorrigirTransacaoDto dto)
            => Ok(MapToReadDto(await _movimentos.Corrigir(id, dto)));
        [HttpPost("{id:int}/desconsideracao")]
        public async Task<ActionResult<TransacaoReadDto>> Desconsideracao(int id, DesconsiderarTransacaoDto dto)
            => Ok(MapToReadDto(await _movimentos.Desconsiderar(id, dto)));
        [HttpPut("{id:int}/classificacao")]
        public async Task<ActionResult<TransacaoReadDto>> Classificacao(int id, ClassificarHistoricoDto dto)
            => Ok(MapToReadDto(await _movimentos.Classificar(id, dto)));
        [HttpGet("{id:int}/revisoes")]
        public async Task<IActionResult> Revisoes(int id)
        {
            if (!await _context.Transacoes.AnyAsync(t => t.Id == id)) return NotFound();
            return Ok(await _context.RevisoesTransacoes.AsNoTracking().Where(r => r.TransacaoId == id)
                .OrderBy(r => r.Id).Select(r => new { r.Id, r.Instante, r.Motivo, r.Antes, r.Depois }).ToListAsync());
        }

        public static TransacaoReadDto MapToReadDto(Transacao t)
        {
            return new TransacaoReadDto
            {
                Id = t.Id,
                ContaId = t.ContaId,
                ContaNome = t.Conta?.Nome,
                Estado = t.Estado,
                OrigemRegistro = t.OrigemRegistro,
                ClassificacaoPendente = t.ClassificacaoPendente,
                DataEfetivacao = t.DataEfetivacao,
                Versao = Convert.ToBase64String(t.Versao),
                CreditoLegado = MovimentacaoService.EstruturaCredito(t),
                MotivoDesconsideracao = t.MotivoDesconsideracao,
                Titulo = t.Titulo,
                Valor = t.Valor,
                Data = t.Data,
                Tipo = t.Tipo,
                Categoria = t.Categoria,
                MetodoPagamento = t.MetodoPagamento,
                NumeroParcelas = t.NumeroParcelas,
                ParcelaAtual = t.ParcelaAtual,
                TransacaoOrigemId = t.TransacaoOrigemId
            };
        }
    }
}
