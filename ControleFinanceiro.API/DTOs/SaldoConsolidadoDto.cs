namespace ControleFinanceiro.API.DTOs;
public class SaldoConsolidadoDto {
    public DateOnly Referencia { get; set; }
    public List<SaldoContaDto> Contas { get; set; } = [];
    public decimal? TotalCalculavel => Contas.Any(c => c.Calculavel) ? Contas.Where(c => c.Calculavel).Sum(c => c.SaldoCalculado!.Value) : null;
    public bool Parcial => Contas.Any(c => !c.Calculavel);
    public List<SaldoContaDto> ContasExcluidas => Contas.Where(c => !c.Calculavel).ToList();
    public int PendentesRevisao { get; set; }
}
