using ControleFinanceiro.API.Enums;
namespace ControleFinanceiro.API.Models;
public class Previsao
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public TipoTransacao Tipo { get; set; }
    public decimal ValorPrevistoOriginal { get; set; }
    public decimal? ValorFinal { get; set; }
    public DateOnly DataPrevista { get; set; }
    public int? ContaId { get; set; }
    public Conta? Conta { get; set; }
    public string Categoria { get; set; } = "";
    public string? Observacoes { get; set; }
    public int? AnoCompetencia { get; set; }
    public int? MesCompetencia { get; set; }
    public EstadoPrevisao Estado { get; set; } = EstadoPrevisao.Ativa;
    public DateTimeOffset? EncerradaEm { get; set; }
    public DateTimeOffset? CanceladaEm { get; set; }
    public string? MotivoEncerramento { get; set; }
    public string? MotivoCancelamento { get; set; }
    public byte[] Versao { get; set; } = [];
    public ICollection<Transacao> Transacoes { get; set; } = new List<Transacao>();
}
