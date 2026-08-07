using ExpenseTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public interface ICurrencyService
{
    Task<string> GetDefaultAsync(int userId);
    Task<Dictionary<string, decimal>> GetRatesToDefaultAsync(int userId);
    decimal Convert(decimal amount, string fromCurrency, string toCurrency, IReadOnlyDictionary<string, decimal> rates);
}

public class CurrencyService : ICurrencyService
{
    private readonly AppDbContext _db;

    public CurrencyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetDefaultAsync(int userId) =>
        await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CurrencyCode)
            .SingleAsync();

    public async Task<Dictionary<string, decimal>> GetRatesToDefaultAsync(int userId) =>
        await _db.CurrencyRates.AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToDictionaryAsync(r => r.FromCurrency, r => r.RateToDefault);

    public decimal Convert(decimal amount, string fromCurrency, string toCurrency, IReadOnlyDictionary<string, decimal> rates)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var rate = rates.FirstOrDefault(kv =>
            string.Equals(kv.Key, fromCurrency, StringComparison.OrdinalIgnoreCase)).Value;
        if (rate == 0)
        {
            rate = 1m;
        }

        return Math.Round(amount * rate, 2);
    }
}
