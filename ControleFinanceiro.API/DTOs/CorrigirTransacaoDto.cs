using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class CorrigirTransacaoDto : TransacaoRealizadaCreateDto {
    [Required, MaxLength(500)] public string Motivo { get; set; } = "";
    [Required] public string Versao { get; set; } = "";
}
