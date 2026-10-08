using ControleFinanceiro.API.Services;
using ControleFinanceiro.API.Data;
using ControleFinanceiro.API.DTOs;
using ControleFinanceiro.API.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly SaldoService _saldos;
        private readonly PrevisaoService _previsoes;

        public DashboardController(AppDbContext context, SaldoService saldos, PrevisaoService previsoes)
        {
            _context = context;
            _saldos = saldos;
            _previsoes = previsoes;
        }

        private static readonly string[] NomesMeses =
        {
            "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
            "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
        };

        // ─────────────────────────────────────────────────────────────
        // GET: api/dashboard?mes=8&ano=2026
        // Retorna o DTO consolidado do dashboard.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Retorna o dashboard financeiro consolidado para um mês/ano específicos.
        /// Inclui saldo, receitas, despesas, previsões e gastos por categoria.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DashboardDto>> GetDashboard(
            [FromQuery] int? mes, [FromQuery] int? ano)
        {
            // Usar mês/ano atuais se não informados
            var agora = _saldos.Hoje;
            int mesFiltro = mes ?? agora.Month;
            int anoFiltro = ano ?? agora.Year;

            if (mesFiltro < 1 || mesFiltro > 12 || anoFiltro < 1900 || anoFiltro > 2100)
                return BadRequest(new { mensagem = "O mês deve ser entre 1 e 12." });

            // ── Buscar todas as transações ──
            var todasTransacoes = await _context.Transacoes
                .Include(t => t.Conta)
                .AsNoTracking()
                .ToListAsync();

            // ── Transações do mês selecionado ──
            var transacoesDoMes = todasTransacoes
                .Where(t => t.Data.Month == mesFiltro && t.Data.Year == anoFiltro)
                .ToList();

            // ── Saldo geral (todas as receitas - todas as despesas até o final do mês selecionado) ──
            var ultimoDiaMes = new DateTime(anoFiltro, mesFiltro, DateTime.DaysInMonth(anoFiltro, mesFiltro), 23, 59, 59);
            var transacoesAteAgora = todasTransacoes
                .Where(t => t.Data <= ultimoDiaMes)
                .ToList();

            decimal totalReceitasGeral = transacoesAteAgora
                .Where(t => t.Tipo == TipoTransacao.Receita)
                .Sum(t => t.Valor);

            decimal totalDespesasGeral = transacoesAteAgora
                .Where(t => t.Tipo == TipoTransacao.Despesa)
                .Sum(t => t.Valor);

            decimal saldoConta = totalReceitasGeral - totalDespesasGeral;

            // ── Recebido no mês ──
            decimal recebidoNoMes = transacoesDoMes
                .Where(t => t.Tipo == TipoTransacao.Receita)
                .Sum(t => t.Valor);

            // ── Gasto no mês ──
            decimal gastoNoMes = transacoesDoMes
                .Where(t => t.Tipo == TipoTransacao.Despesa)
                .Sum(t => t.Valor);

            // ── Previsto para gastar (despesas futuras programadas a partir do mês seguinte) ──
            var inicioProximoMes = new DateTime(anoFiltro, mesFiltro, 1).AddMonths(1);
            decimal previstoParaGastar = todasTransacoes
                .Where(t => t.Tipo == TipoTransacao.Despesa && t.Data >= inicioProximoMes)
                .Sum(t => t.Valor);

            // ── Gastos por categoria no mês ──
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

            // ── Previsão dos próximos 3 meses ──
            var previsaoProximosMeses = new List<PrevisaoMensalDto>();
            for (int i = 1; i <= 3; i++)
            {
                var dataRef = new DateTime(anoFiltro, mesFiltro, 1).AddMonths(i);
                var despesasFuturas = todasTransacoes
                    .Where(t => t.Tipo == TipoTransacao.Despesa
                             && t.Data.Month == dataRef.Month
                             && t.Data.Year == dataRef.Year)
                    .ToList();

                previsaoProximosMeses.Add(new PrevisaoMensalDto
                {
                    Mes = dataRef.Month,
                    Ano = dataRef.Year,
                    MesNome = NomesMeses[dataRef.Month],
                    TotalPrevisto = despesasFuturas.Sum(t => t.Valor),
                    QuantidadeParcelas = despesasFuturas.Count
                });
            }

            // ── Salário vigente ──
            var salarioAtivo = await _context.Salarios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Ativo);

            SalarioReadDto? salarioDto = salarioAtivo != null
                ? new SalarioReadDto
                {
                    Id = salarioAtivo.Id,
                    Valor = salarioAtivo.Valor,
                    DiaPagamento = salarioAtivo.DiaPagamento,
                    Ativo = salarioAtivo.Ativo,
                    DataInicio = salarioAtivo.DataInicio,
                    DataFim = salarioAtivo.DataFim
                }
                : null;

            // ── Transações do mês para a lista ──
            var transacoesDtoList = transacoesDoMes
                .OrderByDescending(t => t.Data)
                .ThenByDescending(t => t.Id)
                .Select(TransacoesController.MapToReadDto)
                .ToList();

            // ── Montar resposta ──
            var dashboard = new DashboardDto
            {
                Financeiro = await _saldos.Resumo(mesFiltro, anoFiltro),
                Previsoes = await _previsoes.Resumo(anoFiltro, mesFiltro),
                SaldoConta = saldoConta,
                RecebidoNoMes = recebidoNoMes,
                GastoNoMes = gastoNoMes,
                PrevistoParaGastar = previstoParaGastar,
                Mes = mesFiltro,
                Ano = anoFiltro,
                GastosPorCategoria = gastosPorCategoria,
                PrevisaoProximosMeses = previsaoProximosMeses,
                SalarioAtual = salarioDto,
                TransacoesDoMes = transacoesDtoList
            };

            return Ok(dashboard);
        }
    }
}
