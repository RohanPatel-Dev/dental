using Dental.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Scheduling.Contracts.v1.Providers.CreateProvider;

/// <summary>Adds a provider to the practice.</summary>
/// <param name="UserId">Linked staff account, when there is one.</param>
/// <param name="DisplayName">Name shown on the appointment book.</param>
/// <param name="Speciality">Their speciality.</param>
/// <param name="IsAcceptingPatients">Whether new patients may be booked with them.</param>
public sealed record CreateProviderCommand(
    Guid? UserId,
    string DisplayName,
    string? Speciality,
    bool IsAcceptingPatients) : ICommand<ProviderDto>;
