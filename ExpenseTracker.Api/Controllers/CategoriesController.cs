using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        var userId = this.UserId();
        var categories = await _db.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var counts = await _db.Expenses
            .Where(e => e.UserId == userId && e.CategoryId != null)
            .GroupBy(e => e.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return Ok(categories.Select(c => ToDto(c, counts.GetValueOrDefault(c.Id))));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request)
    {
        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.UserId == this.UserId() && c.Name == name))
        {
            return Conflict(new { message = "A category with this name already exists." });
        }

        var category = new Category
        {
            UserId = this.UserId(),
            Name = name,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#9e9e9e" : request.Color
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), ToDto(category, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request)
    {
        var userId = this.UserId();
        var category = await _db.Categories.SingleOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name == name && c.Id != id))
        {
            return Conflict(new { message = "A category with this name already exists." });
        }

        category.Name = name;
        if (!string.IsNullOrWhiteSpace(request.Color))
        {
            category.Color = request.Color;
        }

        await _db.SaveChangesAsync();

        var count = await _db.Expenses.CountAsync(e => e.CategoryId == category.Id);
        return Ok(ToDto(category, count));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.UserId();
        var category = await _db.Categories.SingleOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category is null)
        {
            return NotFound();
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static CategoryDto ToDto(Category c, int expenseCount) =>
        new(c.Id, c.Name, c.Color, expenseCount);
}
