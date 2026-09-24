namespace ControleFinanceiro.API.DTOs
{
    /// <summary>
    /// DTO consolidado do balanço financeiro mensal.
    /// </summary>
    public class BalancoMensalDto
    {
        public string Aviso => "Resumo dos lançamentos cadastrados. Estes valores ainda não representam saldo bancário confirmado.";
        public bool SaldoBancarioConfirmado => false;
        public int Mes { get; set; }
        public int Ano { get; set; }
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal SaldoFinal { get; set; }
        public List<GastoPorCategoriaDto> GastosPorCategoria { get; set; } = new();
    }

    /// <summary>
    /// Representa o total gasto em uma categoria específica.
    /// </summary>
    public class GastoPorCategoriaDto
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
