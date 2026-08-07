using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/currencies")]
public class CurrenciesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CurrenciesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<UserCurrencyDto>> GetAll()
    {
        var userId = this.UserId();
        var user = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        var rates = await _db.CurrencyRates.AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToDictionaryAsync(r => r.FromCurrency, r => r.RateToDefault);

        var currencies = Currencies.Supported
            .Select(c => new CurrencyInfoDto(c.Code, c.Name, c.Symbol, rates.GetValueOrDefault(c.Code, 1m)))
            .ToList();

        return Ok(new UserCurrencyDto(user.CurrencyCode, currencies));
    }

    [HttpPut("default")]
    public async Task<ActionResult<UserCurrencyDto>> UpdateDefault(UpdateDefaultCurrencyRequest request)
    {
        var userId = this.UserId();
        if (!Currencies.IsSupported(request.DefaultCurrency))
        {
            return BadRequest(new { message = "Unsupported currency code." });
        }

        var user = await _db.Users.SingleAsync(u => u.Id == userId);
        user.CurrencyCode = request.DefaultCurrency;

        var existingRates = await _db.CurrencyRates.Where(r => r.UserId == userId).ToListAsync();
        _db.CurrencyRates.RemoveRange(existingRates);

        await _db.SaveChangesAsync();
        return await GetAll();
    }

    [HttpPut("rates")]
    public async Task<ActionResult<UserCurrencyDto>> UpdateRates(UpdateRatesRequest request)
    {
        var userId = this.UserId();
        var user = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

        foreach (var (currency, rate) in request.Rates)
        {
            if (!Currencies.IsSupported(currency) ||
                string.Equals(currency, user.CurrencyCode, StringComparison.OrdinalIgnoreCase) ||
                rate <= 0)
            {
                return BadRequest(new { message = $"Invalid rate entry for {currency}." });
            }
        }

        foreach (var (currency, rate) in request.Rates)
        {
            var existing = await _db.CurrencyRates
                .SingleOrDefaultAsync(r => r.UserId == userId && r.FromCurrency == currency);

            if (existing is null)
            {
                _db.CurrencyRates.Add(new CurrencyRate
                {
                    UserId = userId,
                    FromCurrency = currency,
                    RateToDefault = rate,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.RateToDefault = rate;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return await GetAll();
    }
}
