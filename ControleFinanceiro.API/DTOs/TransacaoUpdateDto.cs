using System.ComponentModel.DataAnnotations;
using ControleFinanceiro.API.Enums;

namespace ControleFinanceiro.API.DTOs
{
    /// <summary>
    /// Edição dos campos do lançamento legado, sem alterar sua associação de conta.
    /// </summary>
    public class TransacaoUpdateDto
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [MaxLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
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

        [Range(1, 360, ErrorMessage = "O número de parcelas deve ser entre 1 e 360.")]
        public int NumeroParcelas { get; set; } = 1;
    }
}
