using Dental.Framework.Shared.Pagination;
using Dental.Framework.Web.Auth;
using Dental.Modules.Billing.Contracts.Authorization;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.v1.Invoices.SearchInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Billing.Features.v1.Invoices.SearchInvoices;

/// <summary>Maps <c>GET /invoices</c>.</summary>
public static class SearchInvoicesEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSearchInvoicesEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/invoices",
                (
                    [AsParameters] SearchInvoicesRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchInvoicesQuery(
                            request.PatientId,
                            request.Status,
                            request.From,
                            request.To,
                            request.PageNumber ?? PaginationDefaults.MinPageNumber,
                            request.PageSize ?? PaginationDefaults.DefaultPageSize,
                            request.Sort),
                        cancellationToken))
            .WithName("SearchInvoices")
            .WithSummary("Page through invoices")
            .Produces<PagedResponse<InvoiceDto>>(StatusCodes.Status200OK)
            .RequirePermission(BillingPermissions.Invoices.Search);
}

/// <summary>Query string shape for <see cref="SearchInvoicesEndpoint"/>.</summary>
/// <param name="PatientId">Filters to one patient.</param>
/// <param name="Status">Filters by lifecycle status.</param>
/// <param name="From">Earliest creation time.</param>
/// <param name="To">Latest creation time.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchInvoicesRequest(
    Guid? PatientId,
    InvoiceStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int? PageNumber,
    int? PageSize,
    string? Sort);
