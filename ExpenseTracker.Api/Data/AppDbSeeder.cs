using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;

namespace ExpenseTracker.Api.Data;

public static class AppDbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }

        var demo = new User
        {
            Username = "demo",
            Email = "demo@example.com",
            DisplayName = "Demo User",
            PasswordHash = PasswordHasher.Hash("Demo123!")
        };

        db.Users.Add(demo);
        await db.SaveChangesAsync();

        var categories = new List<Category>();
        foreach (var (name, color) in CategoryDefaults.Presets)
        {
            var category = new Category { UserId = demo.Id, Name = name, Color = color };
            db.Categories.Add(category);
            categories.Add(category);
        }

        db.CurrencyRates.AddRange(
            new CurrencyRate { UserId = demo.Id, FromCurrency = "EUR", RateToDefault = 0.92m },
            new CurrencyRate { UserId = demo.Id, FromCurrency = "GBP", RateToDefault = 0.79m },
            new CurrencyRate { UserId = demo.Id, FromCurrency = "INR", RateToDefault = 83.50m });

        await db.SaveChangesAsync();

        var rng = new Random(42);
        var descriptions = new[]
        {
            "Client site visit", "Team lunch", "Stationery order", "Hosting subscription",
            "Electricity bill", "Conference tickets", "Taxi fare", "Printer ink",
            "Domain renewal", "Office rent", "Customer dinner", "Software license"
        };

        var now = DateTime.UtcNow;
        for (int i = 0; i < 60; i++)
        {
            var date = now.AddMonths(-rng.Next(0, 12)).AddDays(-rng.Next(0, 28));
            db.Expenses.Add(new Expense
            {
                UserId = demo.Id,
                CategoryId = categories[rng.Next(categories.Count)].Id,
                Description = descriptions[rng.Next(descriptions.Length)],
                Amount = Math.Round((decimal)(20 + rng.NextDouble() * 800), 2),
                CurrencyCode = rng.Next(10) == 0 ? "EUR" : "USD",
                ExpenseDate = date,
                Notes = rng.Next(4) == 0 ? "Seeded sample expense" : null
            });
        }

        var customers = new[]
        {
            ("Acme Corp", "billing@acme.com", "+1 555-0101", "100 Market St, Springfield"),
            ("Globex Inc.", "ap@globex.io", "+1 555-0102", "42 Industrial Way, Metropolis"),
            ("Initech", "accounts@initech.com", "+1 555-0103", "88 Initech Plaza, San Francisco"),
            ("Umbrella Co.", "finance@umbrella.co", "+1 555-0104", "5 Raccoon Blvd, Detroit")
        };

        var customerEntities = new List<Customer>();
        foreach (var (name, email, phone, address) in customers)
        {
            var customer = new Customer
            {
                UserId = demo.Id,
                Name = name,
                Email = email,
                Phone = phone,
                Address = address
            };
            db.Customers.Add(customer);
            customerEntities.Add(customer);
        }

        await db.SaveChangesAsync();

        var statuses = new[] { InvoiceStatus.Draft, InvoiceStatus.Sent, InvoiceStatus.Paid, InvoiceStatus.Sent };
        for (int i = 0; i < 24; i++)
        {
            var customer = customerEntities[rng.Next(customerEntities.Count)];
            var issue = now.AddMonths(-rng.Next(0, 12)).AddDays(-rng.Next(0, 28));
            var taxRate = 10m;
            var status = statuses[rng.Next(statuses.Length)];

            var invoice = new Invoice
            {
                UserId = demo.Id,
                CustomerId = customer.Id,
                InvoiceNumber = $"INV-{issue.Year}-{i + 1:D4}",
                IssueDate = issue,
                DueDate = issue.AddDays(30),
                Status = status,
                CurrencyCode = rng.Next(10) == 0 ? "EUR" : "USD",
                TaxRate = taxRate,
                Notes = "Seeded sample invoice",
                Items = new List<InvoiceItem>()
            };

            for (int j = 0; j < rng.Next(1, 4); j++)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    Description = $"Consulting service - line {j + 1}",
                    Quantity = rng.Next(1, 10),
                    UnitPrice = Math.Round((decimal)(50 + rng.NextDouble() * 400), 2)
                });
            }

            if (status == InvoiceStatus.Paid)
            {
                invoice.Payments.Add(new InvoicePayment
                {
                    Amount = invoice.Total,
                    PaymentDate = issue.AddDays(10),
                    Method = PaymentMethod.Bank,
                    Reference = "Demo bank transfer"
                });
            }

            db.Invoices.Add(invoice);
        }

        await db.SaveChangesAsync();

        var sequences = db.Invoices
            .Where(i => i.UserId == demo.Id)
            .AsEnumerable()
            .GroupBy(i => i.IssueDate.Year)
            .Select(g => new InvoiceSequence
            {
                UserId = demo.Id,
                Year = g.Key,
                LastNumber = g.Max(i => int.Parse(i.InvoiceNumber.Split('-').Last()))
            });
        db.InvoiceSequences.AddRange(sequences);

        await db.SaveChangesAsync();
    }
}
