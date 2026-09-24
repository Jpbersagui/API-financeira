using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiro.API.DTOs;

public class ContaUpdateDto : ContaCreateDto
{
    [Required]
    public bool? Ativa { get; set; }
}
