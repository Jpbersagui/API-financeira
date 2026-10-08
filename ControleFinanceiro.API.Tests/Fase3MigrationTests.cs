using Microsoft.EntityFrameworkCore;
using Xunit;
namespace ControleFinanceiro.API.Tests;
public class Fase3MigrationTests
{
    [Fact]
    public async Task ExpansivaDesdeFase2PreservaAberturasConfirmacoesRevisoesEParcelas()
    {
        var fixture = new SqlFixture { SeedFase2 = true };
        try
        {
            await fixture.InitializeAsync();
            Assert.Equal(fixture.BeforeMigration, fixture.AfterMigration);
            Assert.Equal(fixture.BeforeFase3, fixture.AfterFase3);
            await using var db = fixture.OpenDb();
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Empty(await db.Previsoes.ToListAsync()); Assert.Empty(await db.RevisoesPrevisoes.ToListAsync());
            Assert.All(await db.Transacoes.ToListAsync(), t => Assert.Null(t.PrevisaoId));
            Assert.Equal(5, (await db.Database.GetAppliedMigrationsAsync()).Count());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally { await fixture.DisposeAsync(); }
    }
}
