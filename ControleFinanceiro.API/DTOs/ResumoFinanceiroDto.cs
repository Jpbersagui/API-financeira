namespace ControleFinanceiro.API.DTOs;
public class ResumoFinanceiroDto {
    public SaldoConsolidadoDto Atual { get; set; } = new();
    public SaldoConsolidadoDto? PosicaoPeriodo { get; set; }
    public DateOnly InicioPeriodo { get; set; }
    public DateOnly? FimPeriodo { get; set; }
    public decimal? EntradasConfirmadas { get; set; }
    public decimal? SaidasConfirmadas { get; set; }
    public string Aviso => "Saldo calculado a partir da abertura e dos movimentos confirmados. Registros aguardando revisão não estão incluídos.";
}
