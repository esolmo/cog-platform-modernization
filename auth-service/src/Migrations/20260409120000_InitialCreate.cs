using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoginName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaxLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DomainEntityId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Indexes ──────────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LoginName",
                table: "Users",
                column: "LoginName",
                unique: true);

            // ── Seed data ─────────────────────────────────────────────────────────

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Name", "Category", "Description" },
                values: new object[,]
                {
                    { 1,  "Wagers.Create",    "Wagers",   "Create new wagers" },
                    { 2,  "Wagers.Read",      "Wagers",   "View wagers" },
                    { 3,  "Wagers.Cancel",    "Wagers",   "Cancel pending wagers" },
                    { 4,  "Wagers.Grade",     "Wagers",   "Grade/settle wagers" },
                    { 5,  "Lines.Read",       "Lines",    "View betting lines" },
                    { 6,  "Lines.Write",      "Lines",    "Set spreads, moneylines, totals" },
                    { 7,  "Lines.Shade",      "Lines",    "Apply/remove per-agent shades" },
                    { 8,  "Accounts.Read",    "Accounts", "View customer account details" },
                    { 9,  "Accounts.Write",   "Accounts", "Edit customer account settings" },
                    { 10, "Accounts.Deposit", "Accounts", "Post deposit transactions" },
                    { 11, "Accounts.Withdraw","Accounts", "Post withdrawal transactions" },
                    { 12, "Agents.Read",      "Agents",   "View agent hierarchy" },
                    { 13, "Agents.Manage",    "Agents",   "Create and manage agents" },
                    { 14, "Reports.View",     "Reports",  "View operational reports" },
                    { 15, "Reports.Export",   "Reports",  "Export report data" },
                    { 16, "Users.Manage",     "Admin",    "Create and manage user accounts" },
                    { 17, "Roles.Manage",     "Admin",    "Assign and revoke roles" },
                    { 18, "System.Config",    "Admin",    "Change system configuration" },
                    { 19, "Lottery.Manage",   "Lottery",  "Manage lottery draws and results" },
                    { 20, "Lottery.Play",     "Lottery",  "Purchase lottery tickets" }
                });

            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name", "Description", "IsActive", "CreatedAt", "CreatedBy" },
                values: new object[,]
                {
                    { 1, "MasterAgent",    "Top-level agent with full access",                    true, seedDate, "seed" },
                    { 2, "Agent",          "Standard agent — manages customers and wagers",       true, seedDate, "seed" },
                    { 3, "SubAgent",       "Sub-agent under a parent agent",                      true, seedDate, "seed" },
                    { 4, "LinesManager",   "Can read and modify betting lines and shades",        true, seedDate, "seed" },
                    { 5, "Admin",          "System administrator — full access to all functions", true, seedDate, "seed" },
                    { 6, "CustomerWeb",    "Customer accessing the web betting frontend",         true, seedDate, "seed" },
                    { 7, "ReportsViewer",  "Read-only access to reports",                         true, seedDate, "seed" },
                    { 8, "LotteryManager", "Lottery game management",                             true, seedDate, "seed" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                values: new object[,]
                {
                    // MasterAgent — all permissions 1–15
                    { 1, 1 }, { 1, 2 }, { 1, 3 }, { 1, 4 }, { 1, 5 },
                    { 1, 6 }, { 1, 7 }, { 1, 8 }, { 1, 9 }, { 1, 10 },
                    { 1, 11 }, { 1, 12 }, { 1, 13 }, { 1, 14 }, { 1, 15 },
                    // Agent
                    { 2, 1 }, { 2, 2 }, { 2, 3 }, { 2, 5 }, { 2, 8 },
                    { 2, 9 }, { 2, 10 }, { 2, 11 }, { 2, 12 }, { 2, 14 },
                    // SubAgent
                    { 3, 1 }, { 3, 2 }, { 3, 5 }, { 3, 8 },
                    // LinesManager
                    { 4, 5 }, { 4, 6 }, { 4, 7 }, { 4, 14 }, { 4, 15 },
                    // Admin — all permissions 1–18
                    { 5, 1 }, { 5, 2 }, { 5, 3 }, { 5, 4 }, { 5, 5 },
                    { 5, 6 }, { 5, 7 }, { 5, 8 }, { 5, 9 }, { 5, 10 },
                    { 5, 11 }, { 5, 12 }, { 5, 13 }, { 5, 14 }, { 5, 15 },
                    { 5, 16 }, { 5, 17 }, { 5, 18 },
                    // CustomerWeb
                    { 6, 1 }, { 6, 2 }, { 6, 5 }, { 6, 20 },
                    // ReportsViewer
                    { 7, 14 }, { 7, 15 },
                    // LotteryManager
                    { 8, 19 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserRoles");
            migrationBuilder.DropTable(name: "RolePermissions");
            migrationBuilder.DropTable(name: "RefreshTokens");
            migrationBuilder.DropTable(name: "Users");
            migrationBuilder.DropTable(name: "Permissions");
            migrationBuilder.DropTable(name: "Roles");
        }
    }
}
