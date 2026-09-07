using Dental.Framework.Core.Exceptions;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Providers.CreateProvider;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;

namespace Dental.Modules.Scheduling.Features.v1.Providers.CreateProvider;

/// <summary>Adds a provider to the practice.</summary>
/// <param name="context">The scheduling context.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
public sealed class CreateProviderCommandHandler(
    SchedulingDbContext context,
    IMultiTenantContextAccessor tenantContextAccessor)
    : ICommandHandler<CreateProviderCommand, ProviderDto>
{
    /// <inheritdoc />
    public async ValueTask<ProviderDto> Handle(
        CreateProviderCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new ForbiddenException("No tenant is resolved for this request.");

        Provider provider = new()
        {
            UserId = command.UserId,
            DisplayName = command.DisplayName.Trim(),
            Speciality = command.Speciality?.Trim(),
            IsAcceptingPatients = command.IsAcceptingPatients,
            TenantId = tenantId,
        };

        context.Providers.Add(provider);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ProviderDto(
            provider.Id,
            provider.UserId,
            provider.DisplayName,
            provider.Speciality,
            provider.IsAcceptingPatients);
    }
}
