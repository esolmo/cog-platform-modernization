namespace ReportsService.Models;

public record WagerActivityRow(
    string DocumentNumber,
    DateTime TranDateTime,
    string TranType,
    decimal Amount,
    string Description,
    int GradeNum);

public record WagerActivityRequest(
    string LoginId,
    DateTime From,
    DateTime To);

public record ChangedTransactionRow(
    int IdCustomer,
    string LoginName,
    DateTime UpdatedDateTime,
    string TransactionType,
    decimal Amount,
    string Description,
    string Reference);

public record ChangedTransactionsRequest(
    int IdAgent,
    int IdCustomer,
    DateTime From,
    DateTime To,
    bool IncludeAgentTransactions);

public record AgentRow(
    int Id,
    string LoginName,
    string FullName,
    string Email,
    bool IsActive);

public record CustomerRow(
    int Id,
    string LoginName,
    string FullName,
    int AgentId,
    decimal Balance);

public record AgentSearchRequest(int AgentId, string Search);

public record PackageTrackerRow(
    int DocumentNumber,
    DateTime TranDate,
    decimal Amount,
    string Status,
    string PackageTo,
    string Reference,
    string PackageService);

public record PackageTrackerRequest(
    int ViewDepartment,
    DateTime From,
    DateTime To,
    int AgentDestination);

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
