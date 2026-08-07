namespace ExpenseTracker.Api.Models;

public enum PaymentMethod
{
    Cash,
    Bank,
    Card,
    Other
}

public class InvoicePayment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Bank;
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Invoice? Invoice { get; set; }
}
