using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;

namespace ExpenseTracker.Api.Tests;

public class InvoiceWorkflowTests
{
    [Theory]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Sent, true)]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Cancelled, true)]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Paid, false)]
    [InlineData(InvoiceStatus.Draft, InvoiceStatus.Overdue, false)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Paid, true)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Cancelled, true)]
    [InlineData(InvoiceStatus.Sent, InvoiceStatus.Draft, false)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Sent, false)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Draft, false)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Cancelled, false)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Draft, false)]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Sent, false)]
    public void CanTransition_ReturnsExpected(InvoiceStatus current, InvoiceStatus next, bool expected)
    {
        Assert.Equal(expected, InvoiceWorkflow.CanTransition(current, next));
    }

    [Fact]
    public void IsFullyPaid_WhenPaidEqualsTotal() =>
        Assert.True(InvoiceWorkflow.IsFullyPaid(total: 100, paid: 100));

    [Fact]
    public void IsFullyPaid_AllowsRoundingEpsilon() =>
        Assert.True(InvoiceWorkflow.IsFullyPaid(total: 100, paid: 99.995m));

    [Fact]
    public void IsFullyPaid_RejectsShortPayment() =>
        Assert.False(InvoiceWorkflow.IsFullyPaid(total: 100, paid: 99.99m));

    [Fact]
    public void IsFullyPaid_AllowsOverpayment() =>
        Assert.True(InvoiceWorkflow.IsFullyPaid(total: 100, paid: 150));

    [Fact]
    public void EffectiveStatus_IsPaid_WhenFullyPaid_RegardlessOfStoredStatus()
    {
        var status = InvoiceWorkflow.EffectiveStatus(
            InvoiceStatus.Sent, DateTime.Today.AddDays(-5), total: 100, paid: 100);

        Assert.Equal(InvoiceStatus.Paid, status);
    }

    [Fact]
    public void EffectiveStatus_IsOverdue_WhenSentAndPastDue()
    {
        var status = InvoiceWorkflow.EffectiveStatus(
            InvoiceStatus.Sent, DateTime.Today.AddDays(-1), total: 100, paid: 0);

        Assert.Equal(InvoiceStatus.Overdue, status);
    }

    [Fact]
    public void EffectiveStatus_IsSent_WhenSentAndNotDueYet()
    {
        var status = InvoiceWorkflow.EffectiveStatus(
            InvoiceStatus.Sent, DateTime.Today.AddDays(5), total: 100, paid: 0);

        Assert.Equal(InvoiceStatus.Sent, status);
    }

    [Fact]
    public void EffectiveStatus_PreservesDraft_EvenWhenPastDue()
    {
        var status = InvoiceWorkflow.EffectiveStatus(
            InvoiceStatus.Draft, DateTime.Today.AddDays(-10), total: 100, paid: 0);

        Assert.Equal(InvoiceStatus.Draft, status);
    }

    [Fact]
    public void IsEditable_OnlyForDraft()
    {
        Assert.True(InvoiceWorkflow.IsEditable(InvoiceStatus.Draft));
        Assert.False(InvoiceWorkflow.IsEditable(InvoiceStatus.Sent));
        Assert.False(InvoiceWorkflow.IsEditable(InvoiceStatus.Paid));
        Assert.False(InvoiceWorkflow.IsEditable(InvoiceStatus.Cancelled));
        Assert.False(InvoiceWorkflow.IsEditable(InvoiceStatus.Overdue));
    }
}
