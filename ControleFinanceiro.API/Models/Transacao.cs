using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ControleFinanceiro.API.Enums;

namespace ControleFinanceiro.API.Models
{
    /// <summary>
    /// Representa uma transação financeira (receita ou despesa).
    /// </summary>
    public class Transacao
    {
        [Key]
        public int Id { get; set; }

        // Associação organiza o histórico; não confirma pagamento ou recebimento.
        public int? ContaId { get; set; }
        public Conta? Conta { get; set; }
        public EstadoTransacao Estado { get; set; } = EstadoTransacao.NaoReconciliada;
        public OrigemRegistroTransacao? OrigemRegistro { get; set; }
        public ClassificacaoPendenteTransacao? ClassificacaoPendente { get; set; }
        public DateOnly? DataEfetivacao { get; set; }
        public DateTimeOffset? ConfirmadaEm { get; set; }
        public DateTimeOffset? DesconsideradaEm { get; set; }
        public string? MotivoDesconsideracao { get; set; }
        public byte[] Versao { get; set; } = [];

        [Required(ErrorMessage = "O título é obrigatório.")]
        [MaxLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "A data é obrigatória.")]
        public DateTime Data { get; set; }

        [Required(ErrorMessage = "O tipo é obrigatório.")]
        public TipoTransacao Tipo { get; set; }

        [Required(ErrorMessage = "A categoria é obrigatória.")]
        [MaxLength(100, ErrorMessage = "A categoria deve ter no máximo 100 caracteres.")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "O método de pagamento é obrigatório.")]
        public MetodoPagamento MetodoPagamento { get; set; }

        /// <summary>
        /// Número total de parcelas (1 = à vista).
        /// </summary>
        [Range(1, 360, ErrorMessage = "O número de parcelas deve ser entre 1 e 360.")]
        public int NumeroParcelas { get; set; } = 1;

        /// <summary>
        /// Indica qual parcela esta transação representa (ex: 2 de 5).
        /// Null se for pagamento à vista.
        /// </summary>
        public int? ParcelaAtual { get; set; }

        /// <summary>
        /// FK para a transação original que gerou o parcelamento.
        /// Null se for a primeira parcela ou pagamento à vista.
        /// </summary>
        public int? TransacaoOrigemId { get; set; }

        [ForeignKey(nameof(TransacaoOrigemId))]
        public Transacao? TransacaoOrigem { get; set; }

        /// <summary>
        /// Parcelas geradas a partir desta transação.
        /// </summary>
        public ICollection<Transacao> Parcelas { get; set; } = new List<Transacao>();
    }
}
