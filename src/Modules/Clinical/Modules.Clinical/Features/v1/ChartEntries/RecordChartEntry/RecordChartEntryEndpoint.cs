using Dental.Framework.Web.Auth;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.ChartEntries.RecordChartEntry;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Clinical.Features.v1.ChartEntries.RecordChartEntry;

/// <summary>Maps <c>POST /chart-entries</c>.</summary>
public static class RecordChartEntryEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapRecordChartEntryEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/chart-entries",
                (
                    RecordChartEntryCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("RecordChartEntry")
            .WithSummary("Record a finding against one tooth")
            .Produces<ChartEntryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(ClinicalPermissions.Treatment.Create);
}
