using System.ComponentModel.DataAnnotations;
using ControleFinanceiro.API.Enums;
namespace ControleFinanceiro.API.DTOs;
public class TransacaoRealizadaCreateDto {
    [Required, MaxLength(200)] public string Titulo { get; set; } = "";
    [Required] public decimal? Valor { get; set; }
    [Required] public DateOnly? DataEfetivacao { get; set; }
    [Required, Range(1, int.MaxValue)] public int? ContaId { get; set; }
    [Required, EnumDataType(typeof(TipoTransacao))] public TipoTransacao? Tipo { get; set; }
    [Required, MaxLength(100)] public string Categoria { get; set; } = "";
    [Required, EnumDataType(typeof(MetodoPagamento))] public MetodoPagamento? MetodoPagamento { get; set; }
}
