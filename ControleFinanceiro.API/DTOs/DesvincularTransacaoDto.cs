using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class DesvincularTransacaoDto : VincularTransacaoDto
{
    [Required, MaxLength(500)] public string Motivo { get; set; } = "";
}
