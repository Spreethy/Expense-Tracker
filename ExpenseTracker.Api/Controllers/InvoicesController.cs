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
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IInvoiceNumberService _numbers;
    private readonly IInvoicePdfService _pdf;
    private readonly ICurrencyService _currency;

    public InvoicesController(
        AppDbContext db,
        IInvoiceNumberService numbers,
        IInvoicePdfService pdf,
        ICurrencyService currency)
    {
        _db = db;
        _numbers = numbers;
        _pdf = pdf;
        _currency = currency;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null)
    {
        var userId = this.UserId();

        var query = _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .Where(i => i.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var statusValue))
        {
            query = query.Where(i => i.Status == statusValue);
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
    public async Task<ActionResult<InvoiceDetailDto>> GetById(int id)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(ToDetailDto(invoice));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDetailDto>> Create(InvoiceRequest request)
    {
        var userId = this.UserId();
        var defaultCurrency = await _currency.GetDefaultAsync(userId);

        if (!await ValidateAsync(userId, request, defaultCurrency))
        {
            return BadRequest(new { message = "Invalid customer, currency, or status." });
        }

        var invoice = new Invoice
        {
            UserId = userId,
            CustomerId = request.CustomerId,
            InvoiceNumber = await _numbers.GenerateAsync(userId, DateTime.UtcNow.Year),
            IssueDate = request.IssueDate == default ? DateTime.Today : request.IssueDate,
            DueDate = request.DueDate == default ? DateTime.Today.AddDays(30) : request.DueDate,
            Status = ParseStatus(request.Status),
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? defaultCurrency : request.CurrencyCode,
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
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .SingleAsync(i => i.Id == invoice.Id);

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, ToDetailDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InvoiceDetailDto>> Update(int id, InvoiceRequest request)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        if (!InvoiceWorkflow.IsEditable(invoice.Status))
        {
            return BadRequest(new { message = "Only draft invoices can be edited." });
        }

        if (!await ValidateAsync(userId, request, invoice.CurrencyCode))
        {
            return BadRequest(new { message = "Invalid customer, currency, or status." });
        }

        invoice.CustomerId = request.CustomerId;
        invoice.IssueDate = request.IssueDate;
        invoice.DueDate = request.DueDate;
        invoice.Status = ParseStatus(request.Status);
        invoice.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? invoice.CurrencyCode : request.CurrencyCode;
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
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .SingleAsync(i => i.Id == invoice.Id);

        return Ok(ToDetailDto(updated));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<InvoiceDetailDto>> UpdateStatus(int id, InvoiceStatusRequest request)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<InvoiceStatus>(request.Status, true, out var next) ||
            !InvoiceWorkflow.CanTransition(invoice.Status, next))
        {
            return BadRequest(new { message = $"Cannot change status from {invoice.Status} to {request.Status}." });
        }

        if (next == InvoiceStatus.Paid && !InvoiceWorkflow.IsFullyPaid(invoice.Total, invoice.PaidAmount))
        {
            return BadRequest(new { message = "Cannot mark as paid while a balance remains." });
        }

        invoice.Status = next;
        await _db.SaveChangesAsync();

        return Ok(ToDetailDto(invoice));
    }

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<InvoiceDetailDto>> AddPayment(int id, PaymentRequest request)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        if (invoice.Status == InvoiceStatus.Cancelled || invoice.Status == InvoiceStatus.Draft)
        {
            return BadRequest(new { message = "Payments can only be recorded on sent or overdue invoices." });
        }

        var balance = invoice.Balance;
        if (request.Amount > balance + 0.005m)
        {
            return BadRequest(new { message = $"Payment exceeds the remaining balance of {balance:N2}." });
        }

        invoice.Payments.Add(new InvoicePayment
        {
            Amount = request.Amount,
            PaymentDate = request.PaymentDate == default ? DateTime.Today : request.PaymentDate,
            Method = ParseMethod(request.Method),
            Reference = request.Reference
        });

        if (InvoiceWorkflow.IsFullyPaid(invoice.Total, invoice.PaidAmount))
        {
            invoice.Status = InvoiceStatus.Paid;
        }

        await _db.SaveChangesAsync();

        return Ok(ToDetailDto(invoice));
    }

    [HttpDelete("{id:int}/payments/{paymentId:int}")]
    public async Task<ActionResult<InvoiceDetailDto>> RemovePayment(int id, int paymentId)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        var payment = invoice.Payments.SingleOrDefault(p => p.Id == paymentId);
        if (payment is null)
        {
            return NotFound();
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return BadRequest(new { message = "Payments on cancelled invoices cannot be removed." });
        }

        invoice.Payments.Remove(payment);

        if (invoice.Status == InvoiceStatus.Paid && !InvoiceWorkflow.IsFullyPaid(invoice.Total, invoice.PaidAmount))
        {
            invoice.Status = InvoiceStatus.Sent;
        }

        await _db.SaveChangesAsync();

        return Ok(ToDetailDto(invoice));
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var userId = this.UserId();
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (invoice is null)
        {
            return NotFound();
        }

        var user = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        var bytes = _pdf.Generate(invoice, user);

        return File(bytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
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

    private async Task<bool> ValidateAsync(int userId, InvoiceRequest request, string defaultCurrency)
    {
        if (request.CustomerId.HasValue)
        {
            var ownsCustomer = await _db.Customers
                .AnyAsync(c => c.Id == request.CustomerId.Value && c.UserId == userId);
            if (!ownsCustomer)
            {
                return false;
            }
        }

        var currency = string.IsNullOrWhiteSpace(request.CurrencyCode) ? defaultCurrency : request.CurrencyCode;
        if (!Currencies.IsSupported(currency))
        {
            return false;
        }

        var status = ParseStatus(request.Status);
        return status is InvoiceStatus.Draft or InvoiceStatus.Sent;
    }

    private static InvoiceStatus ParseStatus(string? status) =>
        Enum.TryParse<InvoiceStatus>(status, true, out var parsed) ? parsed : InvoiceStatus.Draft;

    private static PaymentMethod ParseMethod(string? method) =>
        Enum.TryParse<PaymentMethod>(method, true, out var parsed) ? parsed : PaymentMethod.Bank;

    private static string EffectiveStatus(Invoice i) =>
        InvoiceWorkflow.EffectiveStatus(i.Status, i.DueDate, i.Total, i.PaidAmount).ToString();

    private static InvoiceDto ToDto(Invoice i) =>
        new(
            i.Id,
            i.InvoiceNumber,
            i.CustomerId,
            i.Customer?.Name,
            i.IssueDate,
            i.DueDate,
            EffectiveStatus(i),
            i.CurrencyCode,
            i.TaxRate,
            i.Subtotal,
            i.Tax,
            i.Total,
            i.PaidAmount,
            i.Balance);

    private static InvoiceDetailDto ToDetailDto(Invoice i) =>
        new(
            i.Id,
            i.InvoiceNumber,
            i.CustomerId,
            i.Customer?.Name,
            i.IssueDate,
            i.DueDate,
            EffectiveStatus(i),
            i.CurrencyCode,
            i.TaxRate,
            i.Subtotal,
            i.Tax,
            i.Total,
            i.PaidAmount,
            i.Balance,
            i.Notes,
            i.Items.Select(item => new InvoiceItemDto(
                item.Id, item.Description, item.Quantity, item.UnitPrice, item.Amount)).ToList(),
            i.Payments.Select(p => new InvoicePaymentDto(
                p.Id, p.Amount, p.PaymentDate, p.Method.ToString(), p.Reference)).ToList());
}
