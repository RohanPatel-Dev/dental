using Dental.Framework.Web.Auth;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.ChartEntries.GetToothChart;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Clinical.Features.v1.ChartEntries.GetToothChart;

/// <summary>Maps <c>GET /patients/{patientId:guid}/tooth-chart</c>.</summary>
public static class GetToothChartEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapGetToothChartEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/patients/{patientId:guid}/tooth-chart",
                (Guid patientId, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new GetToothChartQuery(patientId), cancellationToken))
            .WithName("GetToothChart")
            .WithSummary("Read a patient's tooth chart")
            .Produces<IReadOnlyList<ChartEntryDto>>(StatusCodes.Status200OK)
            .RequirePermission(ClinicalPermissions.Treatment.View);
}
