using Dental.Framework.Web.Auth;
using Dental.Modules.Billing.Contracts.Authorization;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.v1.Invoices.VoidInvoice;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Billing.Features.v1.Invoices.VoidInvoice;

/// <summary>Maps <c>POST /invoices/{id:guid}/void</c>.</summary>
public static class VoidInvoiceEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapVoidInvoiceEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/invoices/{id:guid}/void",
                (
                    Guid id,
                    VoidInvoiceRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(new VoidInvoiceCommand(id, request.Reason), cancellationToken))
            .WithName("VoidInvoice")
            .WithSummary("Cancel an invoice without payment")
            .Produces<InvoiceDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(BillingPermissions.Invoices.Update);
}

/// <summary>Body shape for <see cref="VoidInvoiceEndpoint"/>.</summary>
/// <param name="Reason">Why it is being voided.</param>
public sealed record VoidInvoiceRequest(string Reason);
