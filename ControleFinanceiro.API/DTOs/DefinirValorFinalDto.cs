using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class DefinirValorFinalDto
{
    [Required] public decimal? ValorFinal { get; set; }
    [Required] public string Versao { get; set; } = "";
}
