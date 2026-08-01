using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos;

public record ExpenseDto(
    int Id,
    string Description,
    string Category,
    decimal Amount,
    DateTime ExpenseDate,
    string? Notes);

public record ExpenseRequest(
    [Required, MaxLength(200)] string Description,
    [Required, MaxLength(50)] string Category,
    [Range(0.01, 1_000_000_000)] decimal Amount,
    DateTime ExpenseDate,
    [MaxLength(500)] string? Notes);
