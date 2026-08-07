namespace ExpenseTracker.Api.Services;

public sealed record CurrencyInfo(string Code, string Name, string Symbol);

public static class Currencies
{
    public static readonly IReadOnlyList<CurrencyInfo> Supported = new[]
    {
        new CurrencyInfo("USD", "US Dollar", "$"),
        new CurrencyInfo("EUR", "Euro", "€"),
        new CurrencyInfo("GBP", "British Pound", "£"),
        new CurrencyInfo("INR", "Indian Rupee", "₹"),
        new CurrencyInfo("CAD", "Canadian Dollar", "C$"),
        new CurrencyInfo("AUD", "Australian Dollar", "A$"),
        new CurrencyInfo("JPY", "Japanese Yen", "¥"),
        new CurrencyInfo("CNY", "Chinese Yuan", "¥"),
        new CurrencyInfo("SGD", "Singapore Dollar", "S$"),
        new CurrencyInfo("AED", "UAE Dirham", "د.إ")
    };

    public static bool IsSupported(string code) =>
        !string.IsNullOrWhiteSpace(code) && Supported.Any(c =>
            string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

    public static CurrencyInfo Get(string code) =>
        Supported.FirstOrDefault(c =>
            string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)) ?? new CurrencyInfo(code, code, code);
}
