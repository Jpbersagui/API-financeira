using ControleFinanceiro.API.Enums;

namespace ControleFinanceiro.API.DTOs
{
    /// <summary>
    /// DTO de saída para leitura de uma transação financeira.
    /// </summary>
    public class TransacaoReadDto
    {
        public int Id { get; set; }
        public int? PrevisaoId { get; set; }
        public int? ContaId { get; set; }
        public string? ContaNome { get; set; }
        public bool SemConta => ContaId == null;
        public EstadoTransacao Estado { get; set; }
        public OrigemRegistroTransacao? OrigemRegistro { get; set; }
        public ClassificacaoPendenteTransacao? ClassificacaoPendente { get; set; }
        public DateOnly? DataEfetivacao { get; set; }
        public string Versao { get; set; } = "";
        public bool CreditoLegado { get; set; }
        public string? MotivoDesconsideracao { get; set; }
        public string SituacaoFinanceira => Estado switch { EstadoTransacao.Confirmada => "Confirmado", EstadoTransacao.Desconsiderada => "Desconsiderado", _ => "Não reconciliado" };
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
