using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class AlterarEstadoPrevisaoDto
{
    [Required] public string Versao { get; set; } = "";
    [Required, MaxLength(500)] public string Motivo { get; set; } = "";
}
