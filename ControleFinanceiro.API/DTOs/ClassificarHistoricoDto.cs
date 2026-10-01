using System.ComponentModel.DataAnnotations;
namespace ControleFinanceiro.API.DTOs;
public class ClassificarHistoricoDto {
    [Required] public bool? TransferenciaPropria { get; set; }
    [Required] public string Versao { get; set; } = "";
}
