using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPrevisoesERealizacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrevisaoId",
                table: "Transacoes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Previsoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValorPrevistoOriginal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorFinal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DataPrevista = table.Column<DateOnly>(type: "date", nullable: false),
                    ContaId = table.Column<int>(type: "int", nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnoCompetencia = table.Column<int>(type: "int", nullable: true),
                    MesCompetencia = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EncerradaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CanceladaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoEncerramento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Versao = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Previsoes", x => x.Id);
                    table.CheckConstraint("CK_Previsoes_Competencia", "([AnoCompetencia] IS NULL AND [MesCompetencia] IS NULL) OR ([AnoCompetencia] IS NOT NULL AND [MesCompetencia] IS NOT NULL AND [AnoCompetencia] BETWEEN 1 AND 9999 AND [MesCompetencia] BETWEEN 1 AND 12)");
                    table.CheckConstraint("CK_Previsoes_Estado", "([Estado] = 'Ativa' AND [EncerradaEm] IS NULL AND [CanceladaEm] IS NULL AND [MotivoEncerramento] IS NULL AND [MotivoCancelamento] IS NULL) OR ([Estado] = 'Encerrada' AND [EncerradaEm] IS NOT NULL AND [MotivoEncerramento] IS NOT NULL AND [CanceladaEm] IS NULL AND [MotivoCancelamento] IS NULL) OR ([Estado] = 'Cancelada' AND [CanceladaEm] IS NOT NULL AND [MotivoCancelamento] IS NOT NULL AND [EncerradaEm] IS NULL AND [MotivoEncerramento] IS NULL)");
                    table.CheckConstraint("CK_Previsoes_Valores", "[ValorPrevistoOriginal] > 0 AND ([ValorFinal] IS NULL OR [ValorFinal] >= 0)");
                    table.ForeignKey(
                        name: "FK_Previsoes_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RevisoesPrevisoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrevisaoId = table.Column<int>(type: "int", nullable: false),
                    Instante = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Antes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Depois = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisoesPrevisoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisoesPrevisoes_Previsoes_PrevisaoId",
                        column: x => x.PrevisaoId,
                        principalTable: "Previsoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_PrevisaoId",
                table: "Transacoes",
                column: "PrevisaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Previsoes_ContaId",
                table: "Previsoes",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_Previsoes_DataPrevista_Estado",
                table: "Previsoes",
                columns: new[] { "DataPrevista", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_RevisoesPrevisoes_PrevisaoId",
                table: "RevisoesPrevisoes",
                column: "PrevisaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Previsoes_PrevisaoId",
                table: "Transacoes",
                column: "PrevisaoId",
                principalTable: "Previsoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Previsoes_PrevisaoId",
                table: "Transacoes");

            migrationBuilder.DropTable(
                name: "RevisoesPrevisoes");

            migrationBuilder.DropTable(
                name: "Previsoes");

            migrationBuilder.DropIndex(
                name: "IX_Transacoes_PrevisaoId",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "PrevisaoId",
                table: "Transacoes");
        }
    }
}
