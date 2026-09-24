using System.Text.Json.Serialization;
using ControleFinanceiro.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// ── Entity Framework Core ──
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection com um banco SQL Server persistente antes de iniciar.");

try
{
    var connection = new SqlConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(connection.DataSource) || string.IsNullOrWhiteSpace(connection.InitialCatalog))
        throw new ArgumentException("Servidor e banco são obrigatórios.");
}
catch (ArgumentException ex)
{
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection com servidor e banco SQL Server válidos.", ex);
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ── CORS (permitir frontend acessar a API) ──
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Swagger / OpenAPI ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Controle Financeiro API",
        Version = "v1",
        Description = "API RESTful para gerenciamento de transações financeiras pessoais."
    });
});

var app = builder.Build();

// ── Inicializar banco de dados ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        db.Database.Migrate();
    }
    catch (SqlException ex)
    {
        throw new InvalidOperationException("Não foi possível preparar o banco persistente. Verifique se o SQL Server está em execução e revise ConnectionStrings:DefaultConnection.", ex);
    }
    // Cadastro salarial preservado, sem gerar movimentação automaticamente.
}

// ── Pipeline HTTP ──
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Controle Financeiro API v1");
        options.DocumentTitle = "Controle Financeiro - Swagger";
    });
}

// Servir arquivos estáticos de wwwroot/
app.UseDefaultFiles();   // index.html como página padrão
app.UseStaticFiles();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
