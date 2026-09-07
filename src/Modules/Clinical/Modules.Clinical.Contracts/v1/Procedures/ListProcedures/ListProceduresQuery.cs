using Dental.Modules.Clinical.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Clinical.Contracts.v1.Procedures.ListProcedures;

/// <summary>Lists the practice's procedure catalog. Cached, and small enough not to paginate.</summary>
/// <param name="Category">Filters by category.</param>
/// <param name="OnlyActive">Restricts to procedures that can still be planned.</param>
public sealed record ListProceduresQuery(ProcedureCategory? Category, bool OnlyActive)
    : IQuery<IReadOnlyList<ProcedureDto>>;
