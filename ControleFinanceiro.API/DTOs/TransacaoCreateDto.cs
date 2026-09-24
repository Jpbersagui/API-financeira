using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiro.API.DTOs;

public class TransacaoCreateDto : TransacaoUpdateDto
{
    [Range(1, int.MaxValue)]
    public int? ContaId { get; set; }
}
