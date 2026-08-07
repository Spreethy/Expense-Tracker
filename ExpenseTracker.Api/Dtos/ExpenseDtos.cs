using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos;

public record ExpenseDto(
    int Id,
    string Description,
    int? CategoryId,
    string Category,
    decimal Amount,
    string CurrencyCode,
    DateTime ExpenseDate,
    string? Notes);

public record ExpenseRequest(
    [Required, MaxLength(200)] string Description,
    int? CategoryId,
    [Range(0.01, 1_000_000_000)] decimal Amount,
    [Required, MaxLength(3)] string CurrencyCode,
    DateTime ExpenseDate,
    [MaxLength(500)] string? Notes);
