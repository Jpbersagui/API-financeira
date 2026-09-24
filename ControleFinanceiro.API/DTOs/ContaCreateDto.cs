using System.ComponentModel.DataAnnotations;
using ControleFinanceiro.API.Enums;

namespace ControleFinanceiro.API.DTOs;

public class ContaCreateDto
{
    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required, EnumDataType(typeof(TipoConta))]
    public TipoConta? Tipo { get; set; }
}
