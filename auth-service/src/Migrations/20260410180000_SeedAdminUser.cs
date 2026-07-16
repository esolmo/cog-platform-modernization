using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        // BCrypt hash of "Admin123!" (work factor 11)
        private const string AdminPasswordHash =
            "$2a$11$5dqVr/jVOb7JyZ2On5p8nOaFgifVbN9xqfYdmq3TRAYwX15WzdIQK";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM Users WHERE LoginName = 'admin')
                BEGIN
                    INSERT INTO Users (LoginName, PasswordHash, Email, IsActive, MaxLevel, UserType, CreatedBy, CreatedAt)
                    VALUES ('admin', '{AdminPasswordHash}', 'admin@cog.local', 1, 'Admin', 3, 'seed', '2024-01-01T00:00:00Z');
                    -- UserType 3 = Employee
                END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM UserRoles ur
                    INNER JOIN Users u ON ur.UserId = u.Id
                    WHERE u.LoginName = 'admin' AND ur.RoleId = 5
                )
                BEGIN
                    INSERT INTO UserRoles (UserId, RoleId, AssignedBy, AssignedAt)
                    SELECT Id, 5, 'seed', '2024-01-01T00:00:00Z'
                    FROM   Users
                    WHERE  LoginName = 'admin';
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE ur FROM UserRoles ur
                INNER JOIN Users u ON ur.UserId = u.Id
                WHERE u.LoginName = 'admin';

                DELETE FROM Users WHERE LoginName = 'admin';");
        }
    }
}
