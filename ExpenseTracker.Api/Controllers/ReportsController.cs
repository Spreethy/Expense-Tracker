using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;
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

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary()
    {
        var userId = this.UserId();
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var expenses = await _db.Expenses.Where(e => e.UserId == userId).ToListAsync();
        var invoices = await _db.Invoices.Where(i => i.UserId == userId).ToListAsync();
        var customerCount = await _db.Customers.CountAsync(c => c.UserId == userId);

        var effectiveStatus = (Invoice i) => i.Status == InvoiceStatus.Sent && i.DueDate.Date < today
            ? InvoiceStatus.Overdue
            : i.Status;

        var summary = new DashboardSummary(
            TotalExpensesThisMonth: expenses.Where(e => e.ExpenseDate.Date >= startOfMonth).Sum(e => e.Amount),
            TotalExpensesAllTime: expenses.Sum(e => e.Amount),
            TotalInvoiced: invoices.Where(i => effectiveStatus(i) is not (InvoiceStatus.Draft or InvoiceStatus.Cancelled)).Sum(i => i.Total),
            TotalPaid: invoices.Where(i => effectiveStatus(i) == InvoiceStatus.Paid).Sum(i => i.Total),
            TotalOutstanding: invoices.Where(i => effectiveStatus(i) is InvoiceStatus.Sent or InvoiceStatus.Overdue).Sum(i => i.Total),
            ExpenseCount: expenses.Count,
            InvoiceCount: invoices.Count,
            OverdueInvoiceCount: invoices.Count(i => effectiveStatus(i) == InvoiceStatus.Overdue),
            CustomerCount: customerCount);

        return Ok(summary);
    }

    [HttpGet("expenses-by-category")]
    public async Task<ActionResult<IEnumerable<CategoryTotal>>> GetExpensesByCategory()
    {
        var userId = this.UserId();

        var data = await _db.Expenses
            .Where(e => e.UserId == userId)
            .GroupBy(e => e.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(e => e.Amount), g.Count()))
            .OrderByDescending(x => x.Amount)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("expenses-by-month")]
    public async Task<ActionResult<IEnumerable<MonthTotal>>> GetExpensesByMonth([FromQuery] int months = 12)
    {
        var userId = this.UserId();
        var from = DateTime.Today.AddMonths(-(months - 1));
        from = new DateTime(from.Year, from.Month, 1);

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
                buckets[key] = (v.Amount + e.Amount, v.Count + 1);
            }
        }

        var result = buckets
            .OrderBy(kv => kv.Key.Month)
            .Select(kv => new MonthTotal($"{kv.Key.Month:0000}-{kv.Key.Item2:00}", kv.Value.Amount, kv.Value.Count));

        return Ok(result);
    }

    [HttpGet("invoices-by-month")]
    public async Task<ActionResult<IEnumerable<MonthTotal>>> GetInvoicesByMonth([FromQuery] int months = 12)
    {
        var userId = this.UserId();
        var from = DateTime.Today.AddMonths(-(months - 1));
        from = new DateTime(from.Year, from.Month, 1);

        var invoices = await _db.Invoices
            .Include(i => i.Items)
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
                buckets[key] = (v.Amount + inv.Total, v.Count + 1);
            }
        }

        var result = buckets
            .OrderBy(kv => kv.Key.Month)
            .Select(kv => new MonthTotal($"{kv.Key.Month:0000}-{kv.Key.Item2:00}", kv.Value.Amount, kv.Value.Count));

        return Ok(result);
    }

    [HttpGet("invoices-by-status")]
    public async Task<ActionResult<IEnumerable<StatusTotal>>> GetInvoicesByStatus()
    {
        var userId = this.UserId();
        var today = DateTime.Today;

        var invoices = await _db.Invoices
            .Include(i => i.Items)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var effective = invoices
            .Select(i => new
            {
                Status = i.Status == InvoiceStatus.Sent && i.DueDate.Date < today
                    ? InvoiceStatus.Overdue
                    : i.Status,
                i.Total
            });

        var grouped = effective
            .GroupBy(x => x.Status)
            .Select(g => new StatusTotal(g.Key.ToString(), g.Sum(x => x.Total), g.Count()))
            .OrderBy(x => x.Status)
            .ToList();

        return Ok(grouped);
    }
}
