using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [MaxLength(100)] string DisplayName);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public record AuthResponse(int Id, string Username, string Email, string DisplayName, string CurrencyCode, string Token);
