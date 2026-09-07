using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Billing.Contracts.Authorization;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.v1.Payments.RecordPayment;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Billing.Features.v1.Payments.RecordPayment;

/// <summary>Maps <c>POST /payments</c>.</summary>
public static class RecordPaymentEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapRecordPaymentEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/payments",
                (
                    RecordPaymentCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("RecordPayment")
            .WithSummary("Record a payment against an invoice")
            .Produces<PaymentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(BillingPermissions.Payments.Create)

            // Taking the same payment twice is the failure mode this exists to prevent.
            .WithIdempotency();
}
