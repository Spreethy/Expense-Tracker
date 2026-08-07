using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Dtos;

public record InvoiceItemDto(int Id, string Description, decimal Quantity, decimal UnitPrice, decimal Amount);

public record InvoicePaymentDto(int Id, decimal Amount, DateTime PaymentDate, string Method, string? Reference);

public record InvoiceDto(
    int Id,
    string InvoiceNumber,
    int? CustomerId,
    string? CustomerName,
    DateTime IssueDate,
    DateTime DueDate,
    string Status,
    string CurrencyCode,
    decimal TaxRate,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    decimal PaidAmount,
    decimal Balance);

public record InvoiceDetailDto(
    int Id,
    string InvoiceNumber,
    int? CustomerId,
    string? CustomerName,
    DateTime IssueDate,
    DateTime DueDate,
    string Status,
    string CurrencyCode,
    decimal TaxRate,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    decimal PaidAmount,
    decimal Balance,
    string? Notes,
    List<InvoiceItemDto> Items,
    List<InvoicePaymentDto> Payments);

public record InvoiceRequest(
    int? CustomerId,
    DateTime IssueDate,
    DateTime DueDate,
    string Status,
    [Range(0, 100)] decimal TaxRate,
    [Required, MaxLength(3)] string CurrencyCode,
    [MaxLength(500)] string? Notes,
    List<InvoiceItemRequest> Items);

public record InvoiceItemRequest(
    [Required, MaxLength(200)] string Description,
    [Range(0.01, 1000000)] decimal Quantity,
    [Range(0, 1_000_000_000)] decimal UnitPrice);

public record InvoiceStatusRequest([Required] string Status);

public record PaymentRequest(
    [Range(0.01, 1_000_000_000)] decimal Amount,
    DateTime PaymentDate,
    string? Method,
    [MaxLength(100)] string? Reference);
