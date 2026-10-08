namespace ControleFinanceiro.API.DTOs;
public record TotaisPrevisoesDto(decimal Original, decimal Referencia, decimal Realizado, decimal Restante, decimal Diferenca, decimal Excedente, int Vencidas);
public record ResumoPrevisoesDto(int Ano, int Mes, TotaisPrevisoesDto Receitas, TotaisPrevisoesDto Despesas, int Canceladas)
{
    public string Aviso => "Situação atual das previsões com data prevista no mês. Realizações podem ter ocorrido em outro mês. Não é saldo nem fluxo de caixa do mês; canceladas ficam fora dos totais.";
}
