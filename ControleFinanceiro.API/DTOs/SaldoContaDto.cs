namespace ControleFinanceiro.API.DTOs;
public class SaldoContaDto {
    public int ContaId { get; set; }
    public string Nome { get; set; } = "";
    public bool Ativa { get; set; }
    public DateOnly Referencia { get; set; }
    public DateOnly? DataAbertura { get; set; }
    public decimal? SaldoCalculado { get; set; }
    public bool Calculavel => SaldoCalculado.HasValue;
    public string? MotivoIndisponibilidade { get; set; }
    public int PendentesRevisao { get; set; }
}
