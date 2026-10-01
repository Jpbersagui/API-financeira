using ControleFinanceiro.API.Enums;

namespace ControleFinanceiro.API.DTOs;

public class ContaReadDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoConta Tipo { get; set; }
    public bool Ativa { get; set; }
    public DateOnly? DataAbertura { get; set; }
    public decimal? ValorAbertura { get; set; }
    public string Versao { get; set; } = "";
}
