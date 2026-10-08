using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class VincularTransacaoDto
{
    [Required] public string Versao { get; set; } = "";
    [Required] public string VersaoTransacao { get; set; } = "";
}
