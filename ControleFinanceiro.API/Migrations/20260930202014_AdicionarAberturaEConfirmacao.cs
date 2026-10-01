using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAberturaEConfirmacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transacoes_ContaId",
                table: "Transacoes");

            migrationBuilder.AddColumn<string>(
                name: "ClassificacaoPendente",
                table: "Transacoes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmadaEm",
                table: "Transacoes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataEfetivacao",
                table: "Transacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DesconsideradaEm",
                table: "Transacoes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Transacoes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NaoReconciliada");

            migrationBuilder.AddColumn<string>(
                name: "MotivoDesconsideracao",
                table: "Transacoes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigemRegistro",
                table: "Transacoes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Versao",
                table: "Transacoes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataAbertura",
                table: "Contas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorAbertura",
                table: "Contas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Versao",
                table: "Contas",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "RevisoesTransacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransacaoId = table.Column<int>(type: "int", nullable: false),
                    Instante = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Antes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Depois = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisoesTransacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisoesTransacoes_Transacoes_TransacaoId",
                        column: x => x.TransacaoId,
                        principalTable: "Transacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_ContaId_Estado_DataEfetivacao_Id",
                table: "Transacoes",
                columns: new[] { "ContaId", "Estado", "DataEfetivacao", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transacoes_Confirmacao",
                table: "Transacoes",
                sql: "[Estado] <> 'Confirmada' OR ([ContaId] IS NOT NULL AND [DataEfetivacao] IS NOT NULL AND [ConfirmadaEm] IS NOT NULL AND [Valor] > 0 AND [MetodoPagamento] <> 'CartaoCredito' AND ([OrigemRegistro] IS NULL OR [OrigemRegistro] <> 'CreditoLegado') AND [ClassificacaoPendente] IS NULL AND [NumeroParcelas] = 1 AND [ParcelaAtual] IS NULL AND [TransacaoOrigemId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Contas_Abertura",
                table: "Contas",
                sql: "([DataAbertura] IS NULL AND [ValorAbertura] IS NULL) OR ([DataAbertura] IS NOT NULL AND [ValorAbertura] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_RevisoesTransacoes_TransacaoId",
                table: "RevisoesTransacoes",
                column: "TransacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevisoesTransacoes");

            migrationBuilder.DropIndex(
                name: "IX_Transacoes_ContaId_Estado_DataEfetivacao_Id",
                table: "Transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transacoes_Confirmacao",
                table: "Transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Contas_Abertura",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "ClassificacaoPendente",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "ConfirmadaEm",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "DataEfetivacao",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "DesconsideradaEm",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "MotivoDesconsideracao",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "OrigemRegistro",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "Versao",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "DataAbertura",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "ValorAbertura",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "Versao",
                table: "Contas");

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_ContaId",
                table: "Transacoes",
                column: "ContaId");
        }
    }
}
