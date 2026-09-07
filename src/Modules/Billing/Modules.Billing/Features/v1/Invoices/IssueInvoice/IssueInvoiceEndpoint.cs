using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Billing.Contracts.Authorization;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.v1.Invoices.IssueInvoice;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Billing.Features.v1.Invoices.IssueInvoice;

/// <summary>Maps <c>POST /invoices/{id:guid}/issuance</c>.</summary>
public static class IssueInvoiceEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapIssueInvoiceEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/invoices/{id:guid}/issuance",
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new IssueInvoiceCommand(id), cancellationToken))
            .WithName("IssueInvoice")
            .WithSummary("Present a draft invoice to the patient")
            .Produces<InvoiceDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(BillingPermissions.Invoices.Create)
            .WithIdempotency();
}
