using ExpenseTracker.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ExpenseTracker.Api.Services;

public interface IInvoicePdfService
{
    byte[] Generate(Invoice invoice, User user);
}

public class InvoicePdfService : IInvoicePdfService
{
    public byte[] Generate(Invoice invoice, User user)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(user.DisplayName).FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                        column.Item().Text($"Currency: {invoice.CurrencyCode}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(120).AlignRight().Column(column =>
                    {
                        column.Item().Text("INVOICE").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                        column.Item().Text(invoice.InvoiceNumber).FontSize(12).Bold();
                        column.Item().Text($"Status: {EffectiveStatus(invoice)}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(billTo =>
                        {
                            billTo.Item().Text("BILL TO").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                            if (invoice.Customer is not null)
                            {
                                billTo.Item().Text(invoice.Customer.Name).FontSize(11).Bold();
                                billTo.Item().Text(invoice.Customer.Email);
                                billTo.Item().Text(invoice.Customer.Phone);
                                billTo.Item().Text(invoice.Customer.Address);
                            }
                            else
                            {
                                billTo.Item().Text("—");
                            }
                        });
                        row.RelativeItem().AlignRight().Column(dates =>
                        {
                            dates.Item().Text("Issue date").Bold().FontColor(Colors.Grey.Medium);
                            dates.Item().Text(invoice.IssueDate.ToShortDateString());
                            dates.Item().PaddingTop(4).Text("Due date").Bold().FontColor(Colors.Grey.Medium);
                            dates.Item().Text(invoice.DueDate.ToShortDateString());
                        });
                    });

                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Description").Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Qty").Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Unit price").Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Amount").Bold();
                        });

                        foreach (var item in invoice.Items)
                        {
                            table.Cell().Element(BodyCell).PaddingVertical(3).Text(item.Description);
                            table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString("0.##"));
                            table.Cell().Element(BodyCell).AlignRight().Text(item.UnitPrice.ToString("N2"));
                            table.Cell().Element(BodyCell).AlignRight().Text(item.Amount.ToString("N2"));
                        }
                    });

                    column.Item().AlignRight().Width(220).Column(totals =>
                    {
                        totals.Spacing(3);
                        totals.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Subtotal").FontColor(Colors.Grey.Darken2);
                            row.ConstantItem(90).AlignRight().Text(invoice.Subtotal.ToString("N2"));
                        });
                        totals.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Tax ({invoice.TaxRate:0.##}%)").FontColor(Colors.Grey.Darken2);
                            row.ConstantItem(90).AlignRight().Text(invoice.Tax.ToString("N2"));
                        });
                        totals.Item().PaddingTop(3).Row(row =>
                        {
                            row.RelativeItem().Text("Total").FontSize(13).Bold();
                            row.ConstantItem(90).AlignRight().Text(invoice.Total.ToString("N2")).FontSize(13).Bold();
                        });
                    });

                    if (invoice.Payments.Count > 0)
                    {
                        column.Item().PaddingTop(12).Column(payments =>
                        {
                            payments.Item().Text("PAYMENTS").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                            foreach (var payment in invoice.Payments)
                            {
                                payments.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"{payment.PaymentDate.ToShortDateString()} — {payment.Method} {payment.Reference}");
                                    row.ConstantItem(90).AlignRight().Text($"- {payment.Amount.ToString("N2")}");
                                });
                            }
                            payments.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Balance due").Bold();
                                row.ConstantItem(90).AlignRight().Text(invoice.Balance.ToString("N2")).Bold();
                            });
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Medium));
                    t.Span("Generated by Expense Tracker");
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string EffectiveStatus(Invoice invoice) =>
        InvoiceWorkflow.EffectiveStatus(invoice.Status, invoice.DueDate, invoice.Total, invoice.PaidAmount).ToString();

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(6).BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(6);
}
