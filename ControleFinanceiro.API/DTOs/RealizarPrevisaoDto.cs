using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class RealizarPrevisaoDto : TransacaoRealizadaCreateDto
{
    [Required] public string Versao { get; set; } = "";
}
