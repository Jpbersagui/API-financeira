using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControleFinanceiro.API.Models
{
    /// <summary>
    /// Representa o salário do usuário. Apenas um registro pode estar ativo por vez.
    /// Quando o salário é alterado, o anterior é desativado e um novo é criado.
    /// </summary>
    public class Salario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O valor do salário é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valor { get; set; }

        /// <summary>
        /// Dia do mês em que o salário é recebido (1 a 31).
        /// </summary>
        [Required]
        [Range(1, 31, ErrorMessage = "O dia de pagamento deve ser entre 1 e 31.")]
        public int DiaPagamento { get; set; }

        /// <summary>
        /// Indica se este é o salário vigente.
        /// </summary>
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Data em que este valor de salário passou a valer.
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data em que este salário foi substituído. Null se ainda vigente.
        /// </summary>
        public DateTime? DataFim { get; set; }
    }
}
