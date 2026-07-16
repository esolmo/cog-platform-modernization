namespace AccountsService.Entities;

/// <summary>
/// Agent notes on a customer account.
/// Replaces CommentsForTW / CommentsForCustomer fields and UpdateCommentsFlag in legacy AgICCustManageDet.asp.
/// </summary>
public class CustomerComment
{
    public int      Id              { get; set; }
    public int      CustomerId      { get; set; }
    public string   Body            { get; set; } = string.Empty;
    public bool     VisibleToCustomer { get; set; } = false;  // CommentsForCustomer flag
    public bool     VisibleToAgent  { get; set; } = true;     // CommentsForTW flag
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    public string   CreatedBy       { get; set; } = string.Empty;

    // Navigation
    public Customer Customer { get; set; } = null!;
}
