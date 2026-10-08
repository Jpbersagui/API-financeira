using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ControleFinanceiro.API.Enums;
namespace ControleFinanceiro.API.DTOs;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class PrevisaoCreateDto
{
    [Required, MaxLength(200)] public string Descricao { get; set; } = "";
    [Required, EnumDataType(typeof(TipoTransacao))] public TipoTransacao? Tipo { get; set; }
    [Required] public decimal? ValorPrevistoOriginal { get; set; }
    public decimal? ValorFinal { get; set; }
    [Required] public DateOnly? DataPrevista { get; set; }
    [Range(1, int.MaxValue)] public int? ContaId { get; set; }
    [Required, MaxLength(100)] public string Categoria { get; set; } = "";
    [MaxLength(2000)] public string? Observacoes { get; set; }
    [Range(1, 9999)] public int? AnoCompetencia { get; set; }
    [Range(1, 12)] public int? MesCompetencia { get; set; }
}
