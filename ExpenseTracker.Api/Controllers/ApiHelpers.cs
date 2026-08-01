using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

public static class ApiHelpers
{
    public static int UserId(this ControllerBase controller) =>
        int.Parse(controller.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
