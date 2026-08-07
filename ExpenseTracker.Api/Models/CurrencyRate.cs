namespace ExpenseTracker.Api.Models;

public class CurrencyRate
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public decimal RateToDefault { get; set; } = 1m;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
