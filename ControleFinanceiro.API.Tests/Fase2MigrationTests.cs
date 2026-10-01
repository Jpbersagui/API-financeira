using ControleFinanceiro.API.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace ControleFinanceiro.API.Tests;
public class Fase2MigrationTests
{
    [Fact]
    public async Task MigrationDesdeFase1PreservaTudoSemInferencias()
    {
        var fixture = new SqlFixture { SeedFase1 = true };
        try
        {
            await fixture.InitializeAsync();
            Assert.Equal(fixture.BeforeMigration, fixture.AfterMigration);
            Assert.Equal(fixture.BeforeFase2, fixture.AfterFase2);
            await using var db = fixture.OpenDb();
            var conta = await db.Contas.SingleAsync(); Assert.Null(conta.DataAbertura); Assert.Null(conta.ValorAbertura);
            foreach (var t in await db.Transacoes.ToListAsync())
            {
                Assert.Equal(EstadoTransacao.NaoReconciliada, t.Estado); Assert.Null(t.DataEfetivacao);
                Assert.Null(t.ConfirmadaEm); Assert.Null(t.OrigemRegistro); Assert.Null(t.ClassificacaoPendente);
            }
            Assert.Empty(await db.RevisoesTransacoes.ToListAsync());
            Assert.False(db.Database.HasPendingModelChanges());
        }
        finally { await fixture.DisposeAsync(); }
    }
}
