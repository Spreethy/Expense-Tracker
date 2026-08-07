namespace ExpenseTracker.Api.Dtos;

public record DashboardSummary(
    decimal TotalExpensesThisMonth,
    decimal TotalExpensesAllTime,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal TotalOutstanding,
    int ExpenseCount,
    int InvoiceCount,
    int OverdueInvoiceCount,
    int CustomerCount,
    string Currency);

public record CategoryTotal(int? CategoryId, string Category, decimal Amount, int Count);

public record MonthTotal(string Month, decimal Amount, int Count);

public record StatusTotal(string Status, decimal Amount, int Count);
