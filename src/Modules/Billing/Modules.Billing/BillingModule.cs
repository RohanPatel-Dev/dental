using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Modules.Billing.Contracts.Authorization;
using Dental.Modules.Billing.Contracts.Events;
using Dental.Modules.Billing.Contracts.Services;
using Dental.Modules.Billing.Data;
using Dental.Modules.Billing.Features.v1.Invoices.GetInvoice;
using Dental.Modules.Billing.Features.v1.Invoices.IssueInvoice;
using Dental.Modules.Billing.Features.v1.Invoices.SearchInvoices;
using Dental.Modules.Billing.Features.v1.Invoices.VoidInvoice;
using Dental.Modules.Billing.Features.v1.Payments.RecordPayment;
using Dental.Modules.Billing.Services;
using Dental.Modules.Clinical.Contracts.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(Dental.Modules.Billing.BillingModule), 930)]

namespace Dental.Modules.Billing;

/// <summary>
/// The ledger (order 930). Loads last of the business modules because it reacts to what the others
/// produce rather than originating work itself.
/// </summary>
public sealed class BillingModule : IModule
{
    /// <summary>OpenAPI tag and group name for this module.</summary>
    public const string Tag = "Billing";

    /// <inheritdoc />
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(BillingPermissions.All);

        builder.Services.AddHeroDbContext<BillingDbContext>();
        builder.Services.AddScoped<IDbInitializer, BillingDbInitializer>();

        builder.Services.AddScoped<IInvoiceService, InvoiceService>();
        builder.Services.AddScoped<InvoiceNumberGenerator>();

        // Contributes this module's rows to Clinical's patient timeline without Clinical knowing
        // Billing exists.
        builder.Services.AddScoped<IChartContributor, BillingChartContributor>();

        builder.Services.AddEventingCore(builder.Configuration);
        builder.Services.AddEventingForDbContext<BillingDbContext>();
        builder.Services.AddIntegrationEventHandlers(typeof(BillingModule).Assembly);

        builder.Services.AddIntegrationEventType<InvoiceIssuedIntegrationEvent>();
        builder.Services.AddIntegrationEventType<InvoiceSettledIntegrationEvent>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<BillingDbContext>("db:billing", tags: [HealthEndpoints.ReadyTag]);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapModuleGroup(Tag);

        group.MapSearchInvoicesEndpoint();
        group.MapGetInvoiceEndpoint();
        group.MapIssueInvoiceEndpoint();
        group.MapVoidInvoiceEndpoint();

        group.MapRecordPaymentEndpoint();
    }
}
