using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace BettingService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SportTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoginName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ParentAgentId = table.Column<int>(type: "int", nullable: true),
                    AgentType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agents_Agents_ParentAgentId",
                        column: x => x.ParentAgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SportTypeId = table.Column<int>(type: "int", nullable: false),
                    HomeTeam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AwayTeam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GameDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RotationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_SportTypes_SportTypeId",
                        column: x => x.SportTypeId,
                        principalTable: "SportTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentPermissions_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoginName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsVip = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GamePeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    PeriodDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamePeriods_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TemporaryCreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CasinoBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreePlaysBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBalances_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    MaxWagerStraight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxWagerParlay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxWagerTeaser = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxWagerIfBet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxWagerReverse = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinWager = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxWinPerTicket = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllowStraight = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowParlay = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowTeaser = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowIfBet = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowReverse = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowCasino = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowLottery = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerLimits_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    WagerType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RiskAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualPayout = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TicketNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GradedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GradedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wagers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RelatedWagerId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Wagers_RelatedWagerId",
                        column: x => x.RelatedWagerId,
                        principalTable: "Wagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GamePeriodId = table.Column<int>(type: "int", nullable: false),
                    Spread = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    SpreadJuice = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    HomeMoneyLine = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    AwayMoneyLine = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    OverJuice = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    UnderJuice = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    OfferingSpread = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OfferingMoneyLine = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OfferingTotal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineSets_GamePeriods_GamePeriodId",
                        column: x => x.GamePeriodId,
                        principalTable: "GamePeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WagerItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WagerId = table.Column<int>(type: "int", nullable: false),
                    GamePeriodId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    LineAtTimeOfWager = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HomeScore = table.Column<int>(type: "int", nullable: true),
                    AwayScore = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WagerItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WagerItems_Wagers_WagerId",
                        column: x => x.WagerId,
                        principalTable: "Wagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WagerItems_GamePeriods_GamePeriodId",
                        column: x => x.GamePeriodId,
                        principalTable: "GamePeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineShades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineSetId = table.Column<int>(type: "int", nullable: false),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    SpreadAdjustment = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    HomeMoneyLineAdjustment = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    AwayMoneyLineAdjustment = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    TotalAdjustment = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineShades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineShades_LineSets_LineSetId",
                        column: x => x.LineSetId,
                        principalTable: "LineSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineShades_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Indexes ──────────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(name: "IX_AgentPermissions_AgentId",     table: "AgentPermissions", column: "AgentId");
            migrationBuilder.CreateIndex(name: "IX_Agents_LoginName",              table: "Agents",           column: "LoginName", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Agents_ParentAgentId",          table: "Agents",           column: "ParentAgentId");
            migrationBuilder.CreateIndex(name: "IX_AuditLogs_EntityType_EntityId", table: "AuditLogs",        columns: new[] { "EntityType", "EntityId" });
            migrationBuilder.CreateIndex(name: "IX_AuditLogs_PerformedAt",         table: "AuditLogs",        column: "PerformedAt");
            migrationBuilder.CreateIndex(name: "IX_CustomerBalances_CustomerId",   table: "CustomerBalances", column: "CustomerId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_CustomerLimits_CustomerId",     table: "CustomerLimits",   column: "CustomerId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Customers_AgentId",             table: "Customers",        column: "AgentId");
            migrationBuilder.CreateIndex(name: "IX_Customers_LoginName",           table: "Customers",        column: "LoginName", unique: true);
            migrationBuilder.CreateIndex(name: "IX_GamePeriods_GameId",            table: "GamePeriods",      column: "GameId");
            migrationBuilder.CreateIndex(name: "IX_Games_GameDate",                table: "Games",            column: "GameDate");
            migrationBuilder.CreateIndex(name: "IX_Games_SportTypeId",             table: "Games",            column: "SportTypeId");
            migrationBuilder.CreateIndex(name: "IX_LineShades_AgentId",            table: "LineShades",       column: "AgentId");
            migrationBuilder.CreateIndex(name: "IX_LineShades_LineSetId",          table: "LineShades",       column: "LineSetId");
            migrationBuilder.CreateIndex(name: "IX_LineSets_GamePeriodId",         table: "LineSets",         column: "GamePeriodId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_SportTypes_Code",               table: "SportTypes",       column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Transactions_CustomerId",       table: "Transactions",     column: "CustomerId");
            migrationBuilder.CreateIndex(name: "IX_Transactions_CreatedAt",        table: "Transactions",     column: "CreatedAt");
            migrationBuilder.CreateIndex(name: "IX_Transactions_RelatedWagerId",   table: "Transactions",     column: "RelatedWagerId");
            migrationBuilder.CreateIndex(name: "IX_WagerItems_GamePeriodId",       table: "WagerItems",       column: "GamePeriodId");
            migrationBuilder.CreateIndex(name: "IX_WagerItems_WagerId",            table: "WagerItems",       column: "WagerId");
            migrationBuilder.CreateIndex(name: "IX_Wagers_CreatedAt",              table: "Wagers",           column: "CreatedAt");
            migrationBuilder.CreateIndex(name: "IX_Wagers_CustomerId",             table: "Wagers",           column: "CustomerId");
            migrationBuilder.CreateIndex(
                name: "IX_Wagers_IdempotencyKey",
                table: "Wagers",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            // ── Seed data ─────────────────────────────────────────────────────────

            migrationBuilder.InsertData(
                table: "SportTypes",
                columns: new[] { "Id", "Name", "Code", "IsActive", "DisplayOrder" },
                values: new object[,]
                {
                    { 1,  "Football (NFL)",        "NFL",    true, 1  },
                    { 2,  "Football (NCAAF)",       "NCAAF",  true, 2  },
                    { 3,  "Basketball (NBA)",       "NBA",    true, 3  },
                    { 4,  "Basketball (NCAAB)",     "NCAAB",  true, 4  },
                    { 5,  "Baseball (MLB)",         "MLB",    true, 5  },
                    { 6,  "Hockey (NHL)",           "NHL",    true, 6  },
                    { 7,  "Soccer",                 "SOC",    true, 7  },
                    { 8,  "Boxing / MMA",           "FIGHT",  true, 8  },
                    { 9,  "Tennis",                 "TEN",    true, 9  },
                    { 10, "Golf",                   "GOLF",   true, 10 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LineShades");
            migrationBuilder.DropTable(name: "LineSets");
            migrationBuilder.DropTable(name: "WagerItems");
            migrationBuilder.DropTable(name: "Transactions");
            migrationBuilder.DropTable(name: "Wagers");
            migrationBuilder.DropTable(name: "CustomerLimits");
            migrationBuilder.DropTable(name: "CustomerBalances");
            migrationBuilder.DropTable(name: "AgentPermissions");
            migrationBuilder.DropTable(name: "Customers");
            migrationBuilder.DropTable(name: "GamePeriods");
            migrationBuilder.DropTable(name: "Games");
            migrationBuilder.DropTable(name: "Agents");
            migrationBuilder.DropTable(name: "SportTypes");
            migrationBuilder.DropTable(name: "AuditLogs");
        }
    }
}
