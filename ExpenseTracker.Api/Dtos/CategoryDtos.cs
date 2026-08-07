using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos;

public record CategoryDto(int Id, string Name, string? Color, int ExpenseCount);

public record CategoryRequest(
    [Required, MaxLength(50)] string Name,
    [MaxLength(20)] string? Color);
