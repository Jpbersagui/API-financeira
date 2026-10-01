using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class ConfirmarTransacaoDto {
    [Required, Range(1, int.MaxValue)] public int? ContaId { get; set; }
    [Required] public DateOnly? DataEfetivacao { get; set; }
    [Required] public string Versao { get; set; } = "";
}
