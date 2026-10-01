namespace ControleFinanceiro.API.DTOs;
public class ExtratoContaDto {
    public int ContaId { get; set; }
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public DateOnly? InicioAcompanhado { get; set; }
    public DateOnly? DataAbertura { get; set; }
    public decimal? ValorAbertura { get; set; }
    public bool ExibirAbertura { get; set; }
    public decimal? SaldoInicial { get; set; }
    public decimal? SaldoFinal { get; set; }
    public string? Aviso { get; set; }
    public List<LinhaExtratoDto> Movimentos { get; set; } = [];
}
public class LinhaExtratoDto {
    public int Id { get; set; }
    public DateOnly DataEfetivacao { get; set; }
    public string Descricao { get; set; } = "";
    public decimal Entrada { get; set; }
    public decimal Saida { get; set; }
    public decimal Saldo { get; set; }
}
