namespace ControleFinanceiro.API.Models;
public class RevisaoPrevisao
{
    public int Id { get; set; }
    public int PrevisaoId { get; set; }
    public Previsao Previsao { get; set; } = null!;
    public DateTimeOffset Instante { get; set; }
    public string Acao { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string Antes { get; set; } = "";
    public string Depois { get; set; } = "";
}
