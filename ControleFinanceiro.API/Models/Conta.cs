using ControleFinanceiro.API.Enums;

namespace ControleFinanceiro.API.Models;

public class Conta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoConta Tipo { get; set; }
    public bool Ativa { get; set; } = true;
    public DateOnly? DataAbertura { get; set; }
    public decimal? ValorAbertura { get; set; }
    public byte[] Versao { get; set; } = [];
}
