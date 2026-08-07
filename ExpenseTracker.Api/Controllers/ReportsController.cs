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
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrencyService _currency;

    public ReportsController(AppDbContext db, ICurrencyService currency)
    {
        _db = db;
        _currency = currency;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary()
    {
        var userId = this.UserId();
        var defaultCurrency = await _currency.GetDefaultAsync(userId);
        var rates = await _currency.GetRatesToDefaultAsync(userId);

        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var expenses = await _db.Expenses.Where(e => e.UserId == userId).ToListAsync();
        var invoices = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId)
            .ToListAsync();
        var customerCount = await _db.Customers.CountAsync(c => c.UserId == userId);

        decimal ConvertExpense(Expense e) =>
            _currency.Convert(e.Amount, e.CurrencyCode, defaultCurrency, rates);

        decimal ConvertInvoice(Invoice i) =>
            _currency.Convert(i.Total, i.CurrencyCode, defaultCurrency, rates);

        var effectiveStatus = (Invoice i) =>
            InvoiceWorkflow.EffectiveStatus(i.Status, i.DueDate, i.Total, i.PaidAmount);

        var summary = new DashboardSummary(
            TotalExpensesThisMonth: expenses.Where(e => e.ExpenseDate.Date >= startOfMonth).Sum(ConvertExpense),
            TotalExpensesAllTime: expenses.Sum(ConvertExpense),
            TotalInvoiced: invoices.Where(i => effectiveStatus(i) is not (InvoiceStatus.Draft or InvoiceStatus.Cancelled)).Sum(ConvertInvoice),
            TotalPaid: invoices.Where(i => effectiveStatus(i) == InvoiceStatus.Paid).Sum(ConvertInvoice),
            TotalOutstanding: invoices.Where(i => effectiveStatus(i) is InvoiceStatus.Sent or InvoiceStatus.Overdue).Sum(ConvertInvoice),
            ExpenseCount: expenses.Count,
            InvoiceCount: invoices.Count,
            OverdueInvoiceCount: invoices.Count(i => effectiveStatus(i) == InvoiceStatus.Overdue),
            CustomerCount: customerCount,
            Currency: defaultCurrency);

        return Ok(summary);
    }

    [HttpGet("expenses-by-category")]
    public async Task<ActionResult<IEnumerable<CategoryTotal>>> GetExpensesByCategory()
    {
        var userId = this.UserId();
        var defaultCurrency = await _currency.GetDefaultAsync(userId);
        var rates = await _currency.GetRatesToDefaultAsync(userId);

        var expenses = await _db.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId)
            .ToListAsync();

        var data = expenses
            .GroupBy(e => new { e.CategoryId, Name = e.Category?.Name ?? "Other" })
            .Select(g => new CategoryTotal(
                g.Key.CategoryId,
                g.Key.Name,
                g.Sum(e => _currency.Convert(e.Amount, e.CurrencyCode, defaultCurrency, rates)),
                g.Count()))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return Ok(data);
    }

    [HttpGet("expenses-by-month")]
    public async Task<ActionResult<IEnumerable<MonthTotal>>> GetExpensesByMonth([FromQuery] int months = 12)
    {
        var userId = this.UserId();
        var defaultCurrency = await _currency.GetDefaultAsync(userId);
        var rates = await _currency.GetRatesToDefaultAsync(userId);

        var from = new DateTime(DateTime.Today.AddMonths(-(months - 1)).Year, DateTime.Today.AddMonths(-(months - 1)).Month, 1);

        var expenses = await _db.Expenses
            .Where(e => e.UserId == userId && e.ExpenseDate >= from)
            .ToListAsync();

        var buckets = Enumerable.Range(0, months)
            .Select(i => from.AddMonths(i))
            .ToDictionary(d => (d.Year, d.Month), _ => (Amount: 0m, Count: 0));

        foreach (var e in expenses)
        {
            var key = (e.ExpenseDate.Year, e.ExpenseDate.Month);
            if (buckets.TryGetValue(key, out var v))
            {
                buckets[key] = (v.Amount + _currency.Convert(e.Amount, e.CurrencyCode, defaultCurrency, rates), v.Count + 1);
            }
        }

        return Ok(ToMonthTotals(buckets));
    }

    [HttpGet("invoices-by-month")]
    public async Task<ActionResult<IEnumerable<MonthTotal>>> GetInvoicesByMonth([FromQuery] int months = 12)
    {
        var userId = this.UserId();
        var defaultCurrency = await _currency.GetDefaultAsync(userId);
        var rates = await _currency.GetRatesToDefaultAsync(userId);

        var from = new DateTime(DateTime.Today.AddMonths(-(months - 1)).Year, DateTime.Today.AddMonths(-(months - 1)).Month, 1);

        var invoices = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId && i.IssueDate >= from)
            .ToListAsync();

        var buckets = Enumerable.Range(0, months)
            .Select(i => from.AddMonths(i))
            .ToDictionary(d => (d.Year, d.Month), _ => (Amount: 0m, Count: 0));

        foreach (var inv in invoices)
        {
            var key = (inv.IssueDate.Year, inv.IssueDate.Month);
            if (buckets.TryGetValue(key, out var v))
            {
                var amount = _currency.Convert(inv.Total, inv.CurrencyCode, defaultCurrency, rates);
                buckets[key] = (v.Amount + amount, v.Count + 1);
            }
        }

        return Ok(ToMonthTotals(buckets));
    }

    [HttpGet("invoices-by-status")]
    public async Task<ActionResult<IEnumerable<StatusTotal>>> GetInvoicesByStatus()
    {
        var userId = this.UserId();
        var defaultCurrency = await _currency.GetDefaultAsync(userId);
        var rates = await _currency.GetRatesToDefaultAsync(userId);

        var invoices = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var effective = invoices
            .Select(i => new
            {
                Status = InvoiceWorkflow.EffectiveStatus(i.Status, i.DueDate, i.Total, i.PaidAmount),
                Amount = _currency.Convert(i.Total, i.CurrencyCode, defaultCurrency, rates)
            });

        var grouped = effective
            .GroupBy(x => x.Status)
            .Select(g => new StatusTotal(g.Key.ToString(), g.Sum(x => x.Amount), g.Count()))
            .OrderBy(x => x.Status)
            .ToList();

        return Ok(grouped);
    }

    private static IEnumerable<MonthTotal> ToMonthTotals(Dictionary<(int Year, int Month), (decimal Amount, int Count)> buckets) =>
        buckets
            .OrderBy(kv => kv.Key.Year).ThenBy(kv => kv.Key.Month)
            .Select(kv => new MonthTotal($"{kv.Key.Year:0000}-{kv.Key.Month:00}", kv.Value.Amount, kv.Value.Count));
}
