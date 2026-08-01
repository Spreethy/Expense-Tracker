using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public InvoicesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null)
    {
        var userId = this.UserId();

        var query = _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .Where(i => i.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = Enum.TryParse<InvoiceStatus>(status, true, out var statusValue);
            if (parsed)
            {
                query = query.Where(i => i.Status == statusValue);
            }
        }

        if (customerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }

        var invoices = await query
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();

        return Ok(invoices.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(ToDto(invoice));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(InvoiceRequest request)
    {
        var userId = this.UserId();

        if (request.CustomerId.HasValue)
        {
            var ownsCustomer = await _db.Customers
                .AnyAsync(c => c.Id == request.CustomerId.Value && c.UserId == userId);
            if (!ownsCustomer)
            {
                return BadRequest(new { message = "Invalid customer." });
            }
        }

        var invoice = new Invoice
        {
            UserId = userId,
            CustomerId = request.CustomerId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            IssueDate = request.IssueDate == default ? DateTime.Today : request.IssueDate,
            DueDate = request.DueDate == default ? DateTime.Today.AddDays(30) : request.DueDate,
            Status = ParseStatus(request.Status),
            TaxRate = request.TaxRate,
            Notes = request.Notes,
            Items = request.Items.Select(i => new InvoiceItem
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var created = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .SingleAsync(i => i.Id == invoice.Id);

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, ToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Update(int id, InvoiceRequest request)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        if (request.CustomerId.HasValue)
        {
            var ownsCustomer = await _db.Customers
                .AnyAsync(c => c.Id == request.CustomerId.Value && c.UserId == userId);
            if (!ownsCustomer)
            {
                return BadRequest(new { message = "Invalid customer." });
            }
        }

        invoice.CustomerId = request.CustomerId;
        invoice.IssueDate = request.IssueDate;
        invoice.DueDate = request.DueDate;
        invoice.Status = ParseStatus(request.Status);
        invoice.TaxRate = request.TaxRate;
        invoice.Notes = request.Notes;

        _db.InvoiceItems.RemoveRange(invoice.Items);
        invoice.Items = request.Items.Select(i => new InvoiceItem
        {
            Description = i.Description,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        await _db.SaveChangesAsync();

        var updated = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .SingleAsync(i => i.Id == invoice.Id);

        return Ok(ToDto(updated));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<InvoiceDto>> UpdateStatus(int id, InvoiceStatusRequest request)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        invoice.Status = ParseStatus(request.Status);
        await _db.SaveChangesAsync();

        return Ok(ToDto(invoice));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices.SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";
        var count = await _db.Invoices
            .CountAsync(i => i.InvoiceNumber.StartsWith(prefix));

        string number;
        do
        {
            count++;
            number = $"{prefix}{count:D4}";
        } while (await _db.Invoices.AnyAsync(i => i.InvoiceNumber == number));

        return number;
    }

    private static InvoiceStatus ParseStatus(string? status)
    {
        if (Enum.TryParse<InvoiceStatus>(status, true, out var parsed))
        {
            return parsed;
        }

        return InvoiceStatus.Draft;
    }

    private static InvoiceDto ToDto(Invoice i)
    {
        var status = i.Status;
        if (status == InvoiceStatus.Sent && i.DueDate.Date < DateTime.Today)
        {
            status = InvoiceStatus.Overdue;
        }

        return new InvoiceDto(
            i.Id,
            i.InvoiceNumber,
            i.CustomerId,
            i.Customer?.Name,
            i.IssueDate,
            i.DueDate,
            status.ToString(),
            i.TaxRate,
            i.Subtotal,
            i.Tax,
            i.Total,
            i.Notes,
            i.Items.Select(item => new InvoiceItemDto(
                item.Id, item.Description, item.Quantity, item.UnitPrice, item.Amount)).ToList());
    }
}
