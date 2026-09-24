using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiro.API.DTOs
{
    /// <summary>
    /// DTO de entrada para definir/atualizar o salário.
    /// </summary>
    public class SalarioCreateDto
    {
        [Required(ErrorMessage = "O valor do salário é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Valor { get; set; }

        [Required]
        [Range(1, 31, ErrorMessage = "O dia de pagamento deve ser entre 1 e 31.")]
        public int DiaPagamento { get; set; }
    }

    /// <summary>
    /// DTO de saída para leitura do salário.
    /// </summary>
    public class SalarioReadDto
    {
        public int Id { get; set; }
        public decimal Valor { get; set; }
        public int DiaPagamento { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }
}
