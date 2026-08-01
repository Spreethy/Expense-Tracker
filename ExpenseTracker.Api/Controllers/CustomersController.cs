using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var userId = this.UserId();
        var customers = await _db.Customers
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(customers.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var userId = this.UserId();
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (customer is null)
        {
            return NotFound();
        }

        return Ok(ToDto(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CustomerRequest request)
    {
        var customer = new Customer
        {
            UserId = this.UserId(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, ToDto(customer));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, CustomerRequest request)
    {
        var userId = this.UserId();
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (customer is null)
        {
            return NotFound();
        }

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Address = request.Address;

        await _db.SaveChangesAsync();

        return Ok(ToDto(customer));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.UserId();
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (customer is null)
        {
            return NotFound();
        }

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.Name, c.Email, c.Phone, c.Address, c.CreatedAt);
}
