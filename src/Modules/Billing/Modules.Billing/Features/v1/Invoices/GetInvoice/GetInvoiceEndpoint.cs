using Dental.Framework.Web.Auth;
using Dental.Modules.Billing.Contracts.Authorization;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.v1.Invoices.GetInvoice;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Billing.Features.v1.Invoices.GetInvoice;

/// <summary>Maps <c>GET /invoices/{id:guid}</c>.</summary>
public static class GetInvoiceEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapGetInvoiceEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/invoices/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new GetInvoiceQuery(id), cancellationToken))
            .WithName("GetInvoice")
            .WithSummary("Read one invoice")
            .Produces<InvoiceDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(BillingPermissions.Invoices.View);
}
