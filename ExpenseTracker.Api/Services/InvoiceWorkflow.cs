using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Services;

public static class InvoiceWorkflow
{
    private const decimal RoundingEpsilon = 0.005m;

    public static bool CanTransition(InvoiceStatus current, InvoiceStatus next) =>
        current switch
        {
            InvoiceStatus.Draft => next is InvoiceStatus.Sent or InvoiceStatus.Cancelled,
            InvoiceStatus.Sent => next is InvoiceStatus.Paid or InvoiceStatus.Cancelled,
            _ => false
        };

    public static bool IsEditable(InvoiceStatus status) => status == InvoiceStatus.Draft;

    public static bool IsFullyPaid(decimal total, decimal paid) => paid >= total - RoundingEpsilon;

    public static InvoiceStatus EffectiveStatus(InvoiceStatus stored, DateTime dueDate, decimal total, decimal paid)
    {
        if (IsFullyPaid(total, paid))
        {
            return InvoiceStatus.Paid;
        }

        if (stored == InvoiceStatus.Sent && dueDate.Date < DateTime.Today)
        {
            return InvoiceStatus.Overdue;
        }

        return stored;
    }
}
