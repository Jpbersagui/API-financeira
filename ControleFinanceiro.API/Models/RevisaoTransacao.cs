namespace ControleFinanceiro.API.Models;

// Registro simples de mudança explícita; os saldos usam Transacao, nunca estes snapshots.
public class RevisaoTransacao
{
    public int Id { get; set; }
    public int TransacaoId { get; set; }
    public Transacao Transacao { get; set; } = null!;
    public DateTimeOffset Instante { get; set; }
    public string Motivo { get; set; } = "";
    public string Antes { get; set; } = "";
    public string Depois { get; set; } = "";
}
