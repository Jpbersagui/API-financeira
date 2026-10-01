using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class AberturaContaDto {
    [Required] public DateOnly? DataAbertura { get; set; }
    [Required] public decimal? ValorAbertura { get; set; }
    [Required] public string Versao { get; set; } = "";
}
