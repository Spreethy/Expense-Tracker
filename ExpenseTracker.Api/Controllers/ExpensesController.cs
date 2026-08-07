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
[Route("api/expenses")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrencyService _currency;

    public ExpensesController(AppDbContext db, ICurrencyService currency)
    {
        _db = db;
        _currency = currency;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetAll(
        [FromQuery] int? categoryId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var userId = this.UserId();

        var query = _db.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId);

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(e => e.ExpenseDate.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(e => e.ExpenseDate.Month == month.Value);
        }

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        return Ok(expenses.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseDto>> GetById(int id)
    {
        var userId = this.UserId();
        var expense = await _db.Expenses
            .Include(e => e.Category)
            .SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (expense is null)
        {
            return NotFound();
        }

        return Ok(ToDto(expense));
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(ExpenseRequest request)
    {
        var userId = this.UserId();
        if (!await ValidateAsync(userId, request))
        {
            return BadRequest(new { message = "Invalid category or currency." });
        }

        var defaultCurrency = await _currency.GetDefaultAsync(userId);
        var expense = new Expense
        {
            UserId = userId,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? defaultCurrency : request.CurrencyCode,
            ExpenseDate = request.ExpenseDate == default ? DateTime.Today : request.ExpenseDate,
            Notes = request.Notes
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, ToDto(expense));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExpenseDto>> Update(int id, ExpenseRequest request)
    {
        var userId = this.UserId();
        var expense = await _db.Expenses
            .Include(e => e.Category)
            .SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (expense is null)
        {
            return NotFound();
        }

        if (!await ValidateAsync(userId, request))
        {
            return BadRequest(new { message = "Invalid category or currency." });
        }

        expense.Description = request.Description;
        expense.CategoryId = request.CategoryId;
        expense.Amount = request.Amount;
        expense.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? expense.CurrencyCode : request.CurrencyCode;
        expense.ExpenseDate = request.ExpenseDate;
        expense.Notes = request.Notes;

        await _db.SaveChangesAsync();

        return Ok(ToDto(expense));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.UserId();
        var expense = await _db.Expenses.SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (expense is null)
        {
            return NotFound();
        }

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> ValidateAsync(int userId, ExpenseRequest request)
    {
        if (request.CategoryId.HasValue)
        {
            var ownsCategory = await _db.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && c.UserId == userId);
            if (!ownsCategory)
            {
                return false;
            }
        }

        return string.IsNullOrWhiteSpace(request.CurrencyCode) || Currencies.IsSupported(request.CurrencyCode);
    }

    private static ExpenseDto ToDto(Expense e) =>
        new(e.Id, e.Description, e.CategoryId, e.Category?.Name ?? "Other", e.Amount, e.CurrencyCode, e.ExpenseDate, e.Notes);
}
