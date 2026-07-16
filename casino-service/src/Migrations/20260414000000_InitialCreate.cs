using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CasinoPlayers",
                columns: table => new
                {
                    Id               = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId       = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nickname         = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExternalPlayerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CasinoId         = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    RegisteredAt     = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasinoPlayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CasinoTransactions",
                columns: table => new
                {
                    Id                = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CasinoPlayerId    = table.Column<int>(type: "int", nullable: false),
                    TransactionType   = table.Column<int>(type: "int", nullable: false),
                    Amount            = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DocumentNumber    = table.Column<int>(type: "int", nullable: false),
                    TransferReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RemoteReference   = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status            = table.Column<int>(type: "int", nullable: false),
                    CreatedAt         = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt       = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasinoTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CasinoTransactions_CasinoPlayers_CasinoPlayerId",
                        column: x => x.CasinoPlayerId,
                        principalTable: "CasinoPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CasinoPlayers_CustomerId_CasinoId",
                table: "CasinoPlayers",
                columns: new[] { "CustomerId", "CasinoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CasinoTransactions_CasinoPlayerId",
                table: "CasinoTransactions",
                column: "CasinoPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CasinoTransactions_DocumentNumber",
                table: "CasinoTransactions",
                column: "DocumentNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CasinoTransactions");
            migrationBuilder.DropTable(name: "CasinoPlayers");
        }
    }
}
