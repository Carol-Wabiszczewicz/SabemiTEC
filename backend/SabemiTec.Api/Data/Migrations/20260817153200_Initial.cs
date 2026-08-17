using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SabemiTec.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdTransacao = table.Column<string>(type: "text", nullable: false),
                    IdContrato = table.Column<string>(type: "text", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric", nullable: true),
                    DataPagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusRecebido = table.Column<string>(type: "text", nullable: true),
                    PayloadBruto = table.Column<string>(type: "text", nullable: false),
                    StatusProcessamento = table.Column<string>(type: "text", nullable: false),
                    MensagemErro = table.Column<string>(type: "text", nullable: true),
                    RecebidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusContratos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdContrato = table.Column<string>(type: "text", nullable: false),
                    UltimoIdTransacao = table.Column<string>(type: "text", nullable: false),
                    UltimoValor = table.Column<decimal>(type: "numeric", nullable: false),
                    UltimaDataPagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusAtual = table.Column<string>(type: "text", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusContratos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosLog_IdTransacao",
                table: "EventosLog",
                column: "IdTransacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusContratos_IdContrato",
                table: "StatusContratos",
                column: "IdContrato",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosLog");

            migrationBuilder.DropTable(
                name: "StatusContratos");
        }
    }
}
