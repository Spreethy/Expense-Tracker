namespace ExpenseTracker.Api.Models;

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue,
    Cancelled
}

public class Invoice
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal TaxRate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal Subtotal => Items.Sum(i => i.Amount);
    public decimal Tax => Math.Round(Subtotal * TaxRate / 100m, 2);
    public decimal Total => Subtotal + Tax;

    public User? User { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
