using Dental.Framework.Core.Exceptions;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Domain;
using Shouldly;

namespace Dental.Modules.Billing.Tests.Domain;

/// <summary>The money rules. These belong to the aggregate so no handler can bypass them.</summary>
public sealed class InvoiceTests
{
    #region Happy Path

    [Fact]
    public void Issue_Should_MoveADraftToIssued_And_RecordTheTime()
    {
        Invoice invoice = Draft();
        invoice.AddLine(Line(65m));
        DateTimeOffset issuedAt = DateTimeOffset.UtcNow;

        invoice.Issue(issuedAt);

        invoice.Status.ShouldBe(InvoiceStatus.Issued);
        invoice.IssuedAt.ShouldBe(issuedAt);
    }

    [Fact]
    public void ApplyPayment_Should_MoveToPartiallyPaid_When_SomeBalanceRemains()
    {
        Invoice invoice = Issued(100m);

        invoice.ApplyPayment(Payment(40m));

        invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
        invoice.AmountPaid.ShouldBe(40m);
        invoice.Balance.ShouldBe(60m);
    }

    [Fact]
    public void ApplyPayment_Should_SettleTheInvoice_When_TheBalanceReachesZero()
    {
        Invoice invoice = Issued(100m);

        invoice.ApplyPayment(Payment(60m));
        invoice.ApplyPayment(Payment(40m));

        invoice.Status.ShouldBe(InvoiceStatus.Paid);
        invoice.Balance.ShouldBe(0m);
    }

    [Fact]
    public void Total_Should_BeTheSumOfTheLines()
    {
        Invoice invoice = Draft();
        invoice.AddLine(Line(65m));
        invoice.AddLine(Line(110m));

        invoice.Total.ShouldBe(175m);
    }

    #endregion

    #region Exception Cases

    [Fact]
    public void ApplyPayment_Should_Throw_When_ThePaymentExceedsTheBalance()
    {
        // Overpayment is a refund problem, not a billing one; refusing it keeps the ledger honest.
        Invoice invoice = Issued(100m);

        ConflictException exception =
            Should.Throw<ConflictException>(() => invoice.ApplyPayment(Payment(101m)));

        exception.Message.ShouldContain("exceeds the outstanding balance");
    }

    [Fact]
    public void ApplyPayment_Should_Throw_When_TheCurrencyDiffers()
    {
        // Money enforces this: adding 50 EUR to a USD invoice would otherwise produce a wrong total.
        Invoice invoice = Issued(100m);

        Should.Throw<InvalidOperationException>(() => invoice.ApplyPayment(Payment(50m, "EUR")));
    }

    [Fact]
    public void ApplyPayment_Should_Throw_When_TheInvoiceIsVoided()
    {
        Invoice invoice = Draft();
        invoice.AddLine(Line(65m));
        invoice.Void("Raised against the wrong patient.");

        Should.Throw<ConflictException>(() => invoice.ApplyPayment(Payment(10m)));
    }

    [Fact]
    public void Void_Should_Throw_When_MoneyHasAlreadyBeenReceived()
    {
        Invoice invoice = Issued(100m);
        invoice.ApplyPayment(Payment(25m));

        ConflictException exception =
            Should.Throw<ConflictException>(() => invoice.Void("Changed our mind."));

        exception.Message.ShouldContain("refund the payments first");
    }

    [Fact]
    public void Issue_Should_Throw_When_TheInvoiceHasNoCharges()
    {
        Invoice invoice = Draft();

        Should.Throw<ConflictException>(() => invoice.Issue(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AddLine_Should_Throw_Once_TheInvoiceIsIssued()
    {
        // A charge added after issue would make the presented total disagree with the stored one.
        Invoice invoice = Issued(100m);

        Should.Throw<ConflictException>(() => invoice.AddLine(Line(10m)));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ApplyPayment_Should_SettleExactly_When_ThePaymentEqualsTheBalance()
    {
        Invoice invoice = Issued(65m);

        invoice.ApplyPayment(Payment(65m));

        invoice.Status.ShouldBe(InvoiceStatus.Paid);
        invoice.Balance.ShouldBe(0m);
    }

    [Fact]
    public void Balance_Should_BeTheTotalMinusEveryPayment()
    {
        Invoice invoice = Issued(100m);

        invoice.ApplyPayment(Payment(30m));
        invoice.ApplyPayment(Payment(20m));

        invoice.Balance.ShouldBe(50m);
        invoice.AmountPaid.ShouldBe(50m);
    }

    #endregion

    private static Invoice Draft() => new()
    {
        Number = "INV-2026-000001",
        PatientId = Guid.CreateVersion7(),
        Status = InvoiceStatus.Draft,
        Currency = "USD",
        TenantId = "root",
    };

    private static Invoice Issued(decimal amount)
    {
        Invoice invoice = Draft();
        invoice.AddLine(Line(amount));
        invoice.Issue(DateTimeOffset.UtcNow);
        return invoice;
    }

    private static InvoiceLine Line(decimal amount) => new()
    {
        ProcedureCode = "D0120",
        Description = "Periodic oral evaluation",
        Amount = amount,
        TenantId = "root",
    };

    private static Payment Payment(decimal amount, string currency = "USD") => new()
    {
        Amount = amount,
        Currency = currency,
        Method = PaymentMethod.Card,
        ReceivedAt = DateTimeOffset.UtcNow,
        TenantId = "root",
    };
}
