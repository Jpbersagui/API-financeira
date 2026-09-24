using ControleFinanceiro.API.Enums;

namespace ControleFinanceiro.API.DTOs
{
    /// <summary>
    /// DTO de saída para leitura de uma transação financeira.
    /// </summary>
    public class TransacaoReadDto
    {
        public int Id { get; set; }
        public int? ContaId { get; set; }
        public string? ContaNome { get; set; }
        public bool SemConta => ContaId == null;
        // Todos os lançamentos desta fase ainda pertencem ao modelo não reconciliado.
        public string SituacaoFinanceira => "Não reconciliado";
        public string Titulo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public TipoTransacao Tipo { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public MetodoPagamento MetodoPagamento { get; set; }
        public int NumeroParcelas { get; set; }
        public int? ParcelaAtual { get; set; }
        public int? TransacaoOrigemId { get; set; }
    }
}
