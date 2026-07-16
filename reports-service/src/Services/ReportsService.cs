using Microsoft.Data.SqlClient;
using ReportsService.Models;
using System.Data;

namespace ReportsService.Services;

/// <summary>
/// Replaces WebReports DbAccess.cs + DAReports.cs, using raw SQL (parameterized) via EF Core's
/// database connection.  DataSet-based access is replaced with typed record projections.
/// </summary>
public class ReportsDataService(IConfiguration configuration, ILogger<ReportsDataService> logger) : IReportsService
{
    private SqlConnection CreateConnection() =>
        new(configuration.GetConnectionString("DefaultConnection"));

    public async Task<List<WagerActivityRow>> GetWagerActivityAsync(
        WagerActivityRequest request, CancellationToken ct = default)
    {
        var results = new List<WagerActivityRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand("dbo.rptBetMakerActivity", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };
        cmd.Parameters.Add(new SqlParameter("@LoginId", SqlDbType.VarChar, 20) { Value = request.LoginId.ToLower() });
        cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime) { Value = request.From });
        cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime) { Value = request.To.AddDays(1) });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new WagerActivityRow(
                reader.GetString(reader.GetOrdinal("DocumentNumber")),
                reader.GetDateTime(reader.GetOrdinal("TranDateTime")),
                reader.GetString(reader.GetOrdinal("TranType")),
                reader.GetDecimal(reader.GetOrdinal("Amount")),
                reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                reader.IsDBNull(reader.GetOrdinal("GradeNum")) ? 0 : reader.GetInt32(reader.GetOrdinal("GradeNum"))
            ));
        }

        logger.LogInformation("WagerActivity report: {Count} rows for {LoginId}", results.Count, request.LoginId);
        return results;
    }

    public async Task<List<ChangedTransactionRow>> GetChangedTransactionsAsync(
        ChangedTransactionsRequest request, CancellationToken ct = default)
    {
        var results = new List<ChangedTransactionRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        string sql;
        if (request.IdCustomer == 0)
        {
            // Agent-scoped query — uses BFS hierarchy view (mirrors fn_GetSubAgentHierarchyByID logic)
            sql = """
                SELECT u.*, c.LoginName
                FROM dbo.fn_GetSubAgentHierarchyByID(@IdAgent) a
                INNER JOIN Customer c WITH(NOLOCK) ON c.IdAgent = a.idAgent
                    OR (c.idCustomer = a.idAgent AND @IncludeAgentTran = 1)
                INNER JOIN UpdatedCustomerTransaction u WITH(NOLOCK) ON u.idCustomer = c.idCustomer
                WHERE u.UpdatedDateTime >= @from AND u.UpdatedDateTime <= @to
                ORDER BY u.UpdatedDateTime
                """;
        }
        else
        {
            sql = """
                SELECT u.*, c.LoginName
                FROM UpdatedCustomerTransaction u WITH(NOLOCK)
                INNER JOIN Customer c ON u.idCustomer = c.idCustomer
                WHERE u.IdCustomer = @IdCustomer
                  AND u.UpdatedDateTime >= @from AND u.UpdatedDateTime <= @to
                ORDER BY u.UpdatedDateTime
                """;
        }

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 3600 };
        cmd.Parameters.Add(new SqlParameter("@IdAgent", SqlDbType.Int) { Value = request.IdAgent });
        cmd.Parameters.Add(new SqlParameter("@IdCustomer", SqlDbType.Int) { Value = request.IdCustomer });
        cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime) { Value = request.From });
        cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime) { Value = request.To.AddDays(1) });
        cmd.Parameters.Add(new SqlParameter("@IncludeAgentTran", SqlDbType.Bit) { Value = request.IncludeAgentTransactions });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new ChangedTransactionRow(
                reader.GetInt32(reader.GetOrdinal("idCustomer")),
                reader.GetString(reader.GetOrdinal("LoginName")),
                reader.GetDateTime(reader.GetOrdinal("UpdatedDateTime")),
                reader.IsDBNull(reader.GetOrdinal("TranType")) ? string.Empty : reader.GetString(reader.GetOrdinal("TranType")),
                reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Amount")),
                reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                reader.IsDBNull(reader.GetOrdinal("Reference")) ? string.Empty : reader.GetString(reader.GetOrdinal("Reference"))
            ));
        }

        return results;
    }

    public async Task<List<AgentRow>> SearchAgentsAsync(AgentSearchRequest request, CancellationToken ct = default)
    {
        var results = new List<AgentRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand("ICBBSearchAgent", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add(new SqlParameter("@IdAgent", SqlDbType.Int) { Value = request.AgentId });
        cmd.Parameters.Add(new SqlParameter("@Search", SqlDbType.VarChar, 10) { Value = request.Search });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new AgentRow(
                reader.GetInt32(reader.GetOrdinal("IdAgent")),
                reader.GetString(reader.GetOrdinal("AgentLoginName")),
                reader.IsDBNull(reader.GetOrdinal("FullName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FullName")),
                reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                !reader.IsDBNull(reader.GetOrdinal("IsActive")) && reader.GetBoolean(reader.GetOrdinal("IsActive"))
            ));
        }

        return results;
    }

    public async Task<List<CustomerRow>> SearchCustomersAsync(AgentSearchRequest request, CancellationToken ct = default)
    {
        var results = new List<CustomerRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand("ICBBSearchCust", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add(new SqlParameter("@IdAgent", SqlDbType.Int) { Value = request.AgentId });
        cmd.Parameters.Add(new SqlParameter("@Search", SqlDbType.VarChar, 10) { Value = request.Search });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new CustomerRow(
                reader.GetInt32(reader.GetOrdinal("idCustomer")),
                reader.GetString(reader.GetOrdinal("LoginName")),
                reader.IsDBNull(reader.GetOrdinal("FullName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FullName")),
                reader.GetInt32(reader.GetOrdinal("IdAgent")),
                reader.IsDBNull(reader.GetOrdinal("Balance")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Balance"))
            ));
        }

        return results;
    }

    public async Task<List<PackageTrackerRow>> GetPackageTrackerAsync(PackageTrackerRequest request, CancellationToken ct = default)
    {
        var results = new List<PackageTrackerRow>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand("dbo.rptPackageTracker", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add(new SqlParameter("@viewDep", SqlDbType.Int) { Value = request.ViewDepartment });
        cmd.Parameters.Add(new SqlParameter("@fromDate", SqlDbType.DateTime) { Value = request.From });
        cmd.Parameters.Add(new SqlParameter("@toDate", SqlDbType.DateTime) { Value = request.To });
        cmd.Parameters.Add(new SqlParameter("@agentDes", SqlDbType.Int) { Value = request.AgentDestination });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new PackageTrackerRow(
                reader.GetInt32(reader.GetOrdinal("DocumentNumber")),
                reader.GetDateTime(reader.GetOrdinal("TranDate")),
                reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Amount")),
                reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status")),
                reader.IsDBNull(reader.GetOrdinal("PackageTo")) ? string.Empty : reader.GetString(reader.GetOrdinal("PackageTo")),
                reader.IsDBNull(reader.GetOrdinal("Reference")) ? string.Empty : reader.GetString(reader.GetOrdinal("Reference")),
                reader.IsDBNull(reader.GetOrdinal("PackageService")) ? string.Empty : reader.GetString(reader.GetOrdinal("PackageService"))
            ));
        }

        return results;
    }
}
