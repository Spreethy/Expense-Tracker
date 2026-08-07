using ExpenseTracker.Api.Services;

namespace ExpenseTracker.Api.Tests;

public class CurrencyServiceTests
{
    private readonly CurrencyService _service = new(null!);

    [Fact]
    public void Convert_SameCurrency_ReturnsAmountUnchanged()
    {
        var rates = new Dictionary<string, decimal> { ["EUR"] = 0.92m };

        var result = _service.Convert(100, "USD", "USD", rates);

        Assert.Equal(100, result);
    }

    [Fact]
    public void Convert_DifferentCurrency_UsesStoredRate()
    {
        var rates = new Dictionary<string, decimal> { ["EUR"] = 0.92m };

        var result = _service.Convert(100, "EUR", "USD", rates);

        Assert.Equal(92.00m, result);
    }

    [Fact]
    public void Convert_MissingRate_FallsBackToOne()
    {
        var rates = new Dictionary<string, decimal>();

        var result = _service.Convert(100, "GBP", "USD", rates);

        Assert.Equal(100, result);
    }

    [Fact]
    public void Convert_IsCaseInsensitive()
    {
        var rates = new Dictionary<string, decimal> { ["eur"] = 2m };

        var result = _service.Convert(10, "EUR", "USD", rates);

        Assert.Equal(20m, result);
    }

    [Theory]
    [InlineData("USD", true)]
    [InlineData("eur", true)]
    [InlineData("INR", true)]
    [InlineData("XYZ", false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData(null, false)]
    public void Currencies_IsSupported(string? code, bool expected)
    {
        Assert.Equal(expected, Currencies.IsSupported(code!));
    }

    [Fact]
    public void Currencies_Get_ReturnsKnownCurrency()
    {
        var currency = Currencies.Get("EUR");

        Assert.Equal("Euro", currency.Name);
        Assert.Equal("€", currency.Symbol);
    }

    [Fact]
    public void Currencies_Get_UnknownFallsBackToCode()
    {
        var currency = Currencies.Get("XYZ");

        Assert.Equal("XYZ", currency.Name);
    }
}
