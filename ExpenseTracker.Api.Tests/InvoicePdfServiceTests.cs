using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;
using QuestPDF.Infrastructure;

namespace ExpenseTracker.Api.Tests;

public class InvoicePdfServiceTests
{
    static InvoicePdfServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private readonly InvoicePdfService _service = new();

    [Fact]
    public void Generate_ProducesValidPdf()
    {
        var invoice = new Invoice
        {
            Id = 1,
            InvoiceNumber = "INV-2026-0001",
            IssueDate = new DateTime(2026, 8, 1),
            DueDate = new DateTime(2026, 8, 31),
            Status = InvoiceStatus.Sent,
            CurrencyCode = "USD",
            TaxRate = 10,
            Customer = new Customer
            {
                Name = "Acme Corp",
                Email = "billing@acme.com",
                Phone = "+1 555-0101",
                Address = "100 Market St"
            },
            Items =
            {
                new InvoiceItem { Description = "Consulting", Quantity = 2, UnitPrice = 150 }
            }
        };
        var user = new User
        {
            DisplayName = "Demo User",
            CurrencyCode = "USD"
        };

        var bytes = _service.Generate(invoice, user);

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes[..4]));
    }
}
