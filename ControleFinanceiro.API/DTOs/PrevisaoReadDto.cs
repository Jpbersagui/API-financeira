using ControleFinanceiro.API.Enums;
namespace ControleFinanceiro.API.DTOs;
public class PrevisaoReadDto
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public TipoTransacao Tipo { get; set; }
    public decimal ValorPrevistoOriginal { get; set; }
    public decimal? ValorFinal { get; set; }
    public decimal ValorReferencia { get; set; }
    public decimal ValorRealizado { get; set; }
    public decimal ValorRestante { get; set; }
    public decimal Diferenca { get; set; }
    public decimal Excedente { get; set; }
    public DateOnly DataPrevista { get; set; }
    public int? ContaId { get; set; }
    public string? ContaNome { get; set; }
    public bool? ContaAtiva { get; set; }
    public string Categoria { get; set; } = "";
    public string? Observacoes { get; set; }
    public int? AnoCompetencia { get; set; }
    public int? MesCompetencia { get; set; }
    public EstadoPrevisao Estado { get; set; }
    public SituacaoPrevisao Situacao { get; set; }
    public bool Vencida { get; set; }
    public string? MotivoEncerramento { get; set; }
    public string? MotivoCancelamento { get; set; }
    public string Versao { get; set; } = "";
    public List<TransacaoReadDto> Transacoes { get; set; } = [];
}
