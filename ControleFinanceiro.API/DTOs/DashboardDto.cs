namespace ControleFinanceiro.API.DTOs
{
    /// <summary>
    /// DTO consolidado do dashboard financeiro.
    /// </summary>
    public class DashboardDto
    {
        public ResumoFinanceiroDto? Financeiro { get; set; }
        public string Aviso => "Resumo dos lançamentos cadastrados. Estes valores ainda não representam saldo bancário confirmado.";
        public bool SaldoBancarioConfirmado => false;
        /// <summary>
        /// Resultado legado até o fim do mês selecionado. Não é saldo bancário confirmado.
        /// </summary>
        public decimal SaldoConta { get; set; }

        /// <summary>
        /// Receitas cadastradas no mês, sem confirmação de recebimento.
        /// </summary>
        public decimal RecebidoNoMes { get; set; }

        /// <summary>
        /// Despesas cadastradas no mês, sem confirmação de pagamento.
        /// </summary>
        public decimal GastoNoMes { get; set; }

        /// <summary>
        /// Total de parcelas/despesas programadas para os próximos meses.
        /// </summary>
        public decimal PrevistoParaGastar { get; set; }

        /// <summary>
        /// Mês e ano de referência.
        /// </summary>
        public int Mes { get; set; }
        public int Ano { get; set; }

        /// <summary>
        /// Gastos agrupados por categoria no mês.
        /// </summary>
        public List<GastoPorCategoriaDto> GastosPorCategoria { get; set; } = new();

        /// <summary>
        /// Previsão de gastos dos próximos 3 meses.
        /// </summary>
        public List<PrevisaoMensalDto> PrevisaoProximosMeses { get; set; } = new();

        /// <summary>
        /// Salário vigente (null se não configurado).
        /// </summary>
        public SalarioReadDto? SalarioAtual { get; set; }

        /// <summary>
        /// Últimas transações do mês selecionado.
        /// </summary>
        public List<TransacaoReadDto> TransacoesDoMes { get; set; } = new();
    }

    /// <summary>
    /// Previsão de gastos para um mês futuro.
    /// </summary>
    public class PrevisaoMensalDto
    {
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string MesNome { get; set; } = string.Empty;
        public decimal TotalPrevisto { get; set; }
        public int QuantidadeParcelas { get; set; }
    }
}
