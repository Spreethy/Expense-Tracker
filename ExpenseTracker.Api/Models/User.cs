namespace ExpenseTracker.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<CurrencyRate> CurrencyRates { get; set; } = new List<CurrencyRate>();
    public ICollection<InvoiceSequence> InvoiceSequences { get; set; } = new List<InvoiceSequence>();
}
