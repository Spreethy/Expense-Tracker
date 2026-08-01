using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Dtos;

public record InvoiceItemDto(int Id, string Description, decimal Quantity, decimal UnitPrice, decimal Amount);

public record InvoiceDto(
    int Id,
    string InvoiceNumber,
    int? CustomerId,
    string? CustomerName,
    DateTime IssueDate,
    DateTime DueDate,
    string Status,
    decimal TaxRate,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    string? Notes,
    List<InvoiceItemDto> Items);

public record InvoiceRequest(
    int? CustomerId,
    DateTime IssueDate,
    DateTime DueDate,
    string Status,
    [Range(0, 100)] decimal TaxRate,
    [MaxLength(500)] string? Notes,
    List<InvoiceItemRequest> Items);

public record InvoiceItemRequest(
    [Required, MaxLength(200)] string Description,
    [Range(0.01, 1000000)] decimal Quantity,
    [Range(0, 1_000_000_000)] decimal UnitPrice);

public record InvoiceStatusRequest([Required] string Status);
