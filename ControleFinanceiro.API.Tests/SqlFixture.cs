using ControleFinanceiro.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ControleFinanceiro.API.Tests;

public sealed class SqlFixture : IAsyncLifetime
{
    private const string Prefix = "ControleFinanceiro_Fase1_Tests_";
    private readonly string database = Prefix + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; }
    public ApiFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public string BeforeMigration { get; private set; } = "";
    public string AfterMigration { get; private set; } = "";
    public int StartupTransactionCount { get; private set; }
    public int AccountsAfterMigration { get; private set; }

    public SqlFixture()
    {
        ConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = @"localhost\SQLEXPRESS", InitialCatalog = database,
            IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 10
        }.ConnectionString;
    }

    public AppDbContext OpenDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var db = OpenDb();
        await db.GetService<IMigrator>().MigrateAsync("20260807033034_AddSalarios");
        await db.Database.ExecuteSqlRawAsync("""
            SET IDENTITY_INSERT Transacoes ON;
            INSERT INTO Transacoes (Id,Titulo,Valor,Data,Tipo,Categoria,MetodoPagamento,NumeroParcelas,ParcelaAtual,TransacaoOrigemId) VALUES
            (101,N'Histórico comum',80.23,'2026-08-05T12:30:00','Despesa',N'Alimentação','Pix',1,NULL,NULL),
            (102,N'Compra parcela 1',50.01,'2026-08-06','Despesa',N'Tecnologia','CartaoCredito',2,1,NULL),
            (103,N'Compra parcela 2',50.00,'2026-09-06','Despesa',N'Tecnologia','CartaoCredito',2,2,102);
            SET IDENTITY_INSERT Transacoes OFF;
            SET IDENTITY_INSERT Salarios ON;
            INSERT INTO Salarios (Id,Valor,DiaPagamento,Ativo,DataInicio,DataFim) VALUES
            (201,3000.55,1,1,'2026-08-01',NULL),
            (202,2500,5,0,'2026-01-01','2026-07-31');
            SET IDENTITY_INSERT Salarios OFF;
            """);
        BeforeMigration = await Snapshot();
        await db.Database.MigrateAsync();
        AfterMigration = await Snapshot();
        AccountsAfterMigration = await db.Contas.CountAsync();
        Factory = new ApiFactory(ConnectionString);
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        StartupTransactionCount = await db.Transacoes.CountAsync();
    }

    private async Task<string> Snapshot()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT Id,Titulo,Valor,Data,Tipo,Categoria,MetodoPagamento,NumeroParcelas,ParcelaAtual,TransacaoOrigemId
            FROM Transacoes ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES)
            + (SELECT Id,Valor,DiaPagamento,Ativo,DataInicio,DataFim FROM Salarios ORDER BY Id FOR JSON PATH, INCLUDE_NULL_VALUES)
            """;
        return (string)(await command.ExecuteScalarAsync())!;
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory != null) await Factory.DisposeAsync();
        // Nunca aceitar nome de banco externo na rotina de limpeza.
        var target = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        if (target != database || !target.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Limpeza recusada: banco não pertence ao teste.");
        await using var db = OpenDb();
        await db.Database.EnsureDeletedAsync();
    }
}

public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Substituir o registro antes que Program execute Database.Migrate().
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
        });
    }
}
