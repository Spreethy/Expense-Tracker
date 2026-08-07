using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos;

public record CurrencyInfoDto(string Code, string Name, string Symbol, decimal RateToDefault);

public record UserCurrencyDto(string DefaultCurrency, List<CurrencyInfoDto> Currencies);

public record UpdateDefaultCurrencyRequest([Required, MaxLength(3)] string DefaultCurrency);

public record UpdateRatesRequest(Dictionary<string, decimal> Rates);
