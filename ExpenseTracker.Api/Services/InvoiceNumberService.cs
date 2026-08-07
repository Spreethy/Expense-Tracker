using System.Data;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseTracker.Api.Services;

public interface IInvoiceNumberService
{
    Task<string> GenerateAsync(int userId, int year);
}

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly AppDbContext _db;

    public InvoiceNumberService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(int userId, int year)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var sequence = await _db.InvoiceSequences
            .FromSqlInterpolated($"""
                SELECT UserId, Year, LastNumber
                FROM InvoiceSequences WITH (UPDLOCK, ROWLOCK)
                WHERE UserId = {userId} AND Year = {year}
                """)
            .SingleOrDefaultAsync();

        if (sequence is null)
        {
            sequence = new InvoiceSequence { UserId = userId, Year = year, LastNumber = 0 };
            _db.InvoiceSequences.Add(sequence);
        }

        sequence.LastNumber++;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return $"INV-{year}-{sequence.LastNumber:D4}";
    }
}
