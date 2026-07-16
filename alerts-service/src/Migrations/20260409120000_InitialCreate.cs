using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentVipSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    NotificationEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentVipSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WagerNumber = table.Column<int>(type: "int", nullable: false),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CustomerLoginName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InetWagerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WagerType = table.Column<int>(type: "int", nullable: false),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsVipAlert = table.Column<bool>(type: "bit", nullable: false),
                    VipAgentId = table.Column<int>(type: "int", nullable: false),
                    IsSharpAction = table.Column<bool>(type: "bit", nullable: false),
                    IsSquareAction = table.Column<bool>(type: "bit", nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentVipCustomers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentVipSettingsId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CustomerLoginName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentVipCustomers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentVipCustomers_AgentVipSettings_AgentVipSettingsId",
                        column: x => x.AgentVipSettingsId,
                        principalTable: "AgentVipSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertTicketId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertAttributes_AlertTickets_AlertTicketId",
                        column: x => x.AlertTicketId,
                        principalTable: "AlertTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertTicketId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SportKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertDetails_AlertTickets_AlertTicketId",
                        column: x => x.AlertTicketId,
                        principalTable: "AlertTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertDetailAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertDetailId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertDetailAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertDetailAttributes_AlertDetails_AlertDetailId",
                        column: x => x.AlertDetailId,
                        principalTable: "AlertDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Indexes ──────────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_AgentVipCustomers_AgentVipSettingsId",
                table: "AgentVipCustomers",
                column: "AgentVipSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentVipSettings_AgentId",
                table: "AgentVipSettings",
                column: "AgentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertAttributes_AlertTicketId",
                table: "AlertAttributes",
                column: "AlertTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertDetailAttributes_AlertDetailId",
                table: "AlertDetailAttributes",
                column: "AlertDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertDetails_AlertTicketId",
                table: "AlertDetails",
                column: "AlertTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertTickets_AgentId",
                table: "AlertTickets",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertTickets_ExpiresAt",
                table: "AlertTickets",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertTickets_InsertedAt",
                table: "AlertTickets",
                column: "InsertedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AlertDetailAttributes");
            migrationBuilder.DropTable(name: "AlertDetails");
            migrationBuilder.DropTable(name: "AlertAttributes");
            migrationBuilder.DropTable(name: "AlertTickets");
            migrationBuilder.DropTable(name: "AgentVipCustomers");
            migrationBuilder.DropTable(name: "AgentVipSettings");
        }
    }
}
