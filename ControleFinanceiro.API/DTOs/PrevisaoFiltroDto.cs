using System.ComponentModel.DataAnnotations;
using ControleFinanceiro.API.Enums;
namespace ControleFinanceiro.API.DTOs;
public class PrevisaoFiltroDto
{
    public DateOnly? Inicio { get; set; }
    public DateOnly? Fim { get; set; }
    [Range(1, int.MaxValue)] public int? ContaId { get; set; }
    public bool? SemConta { get; set; }
    [EnumDataType(typeof(TipoTransacao))] public TipoTransacao? Tipo { get; set; }
    [EnumDataType(typeof(SituacaoPrevisao))] public SituacaoPrevisao? Situacao { get; set; }
    public bool? Vencida { get; set; }
}
