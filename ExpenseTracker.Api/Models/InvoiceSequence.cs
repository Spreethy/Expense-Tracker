namespace ExpenseTracker.Api.Models;

public class InvoiceSequence
{
    public int UserId { get; set; }
    public int Year { get; set; }
    public int LastNumber { get; set; }

    public User? User { get; set; }
}
