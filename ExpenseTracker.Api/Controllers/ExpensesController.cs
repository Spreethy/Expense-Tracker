using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;
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

    public ExpensesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetAll(
        [FromQuery] string? category = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var userId = this.UserId();

        var query = _db.Expenses.Where(e => e.UserId == userId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(e => e.Category == category);
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

        return Ok(expenses.Select(e => ToDto(e)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseDto>> GetById(int id)
    {
        var userId = this.UserId();
        var expense = await _db.Expenses.SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (expense is null)
        {
            return NotFound();
        }

        return Ok(ToDto(expense));
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(ExpenseRequest request)
    {
        var expense = new Expense
        {
            UserId = this.UserId(),
            Description = request.Description,
            Category = request.Category,
            Amount = request.Amount,
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
        var expense = await _db.Expenses.SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (expense is null)
        {
            return NotFound();
        }

        expense.Description = request.Description;
        expense.Category = request.Category;
        expense.Amount = request.Amount;
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

    private static ExpenseDto ToDto(Expense e) =>
        new(e.Id, e.Description, e.Category, e.Amount, e.ExpenseDate, e.Notes);
}
