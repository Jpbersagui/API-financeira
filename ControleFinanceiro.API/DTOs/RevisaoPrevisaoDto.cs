namespace ControleFinanceiro.API.DTOs;
public record RevisaoPrevisaoDto(int Id, DateTimeOffset Instante, string Acao, string Motivo, string Antes, string Depois);
