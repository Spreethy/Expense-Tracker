using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos;

public record CustomerDto(
    int Id,
    string Name,
    string Email,
    string Phone,
    string Address,
    DateTime CreatedAt);

public record CustomerRequest(
    [Required, MaxLength(150)] string Name,
    [EmailAddress, MaxLength(150)] string Email,
    [MaxLength(30)] string Phone,
    [MaxLength(300)] string Address);
