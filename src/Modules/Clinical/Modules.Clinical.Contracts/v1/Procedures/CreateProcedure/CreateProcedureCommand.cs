using Dental.Modules.Clinical.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Clinical.Contracts.v1.Procedures.CreateProcedure;

/// <summary>Adds a procedure to the catalog.</summary>
/// <param name="Code">Procedure code, e.g. an ADA CDT code.</param>
/// <param name="Description">What the procedure is.</param>
/// <param name="Category">Broad category.</param>
/// <param name="DefaultFee">Default fee charged.</param>
/// <param name="Currency">ISO 4217 currency of the fee.</param>
/// <param name="DefaultDurationMinutes">Chair time normally booked for it.</param>
public sealed record CreateProcedureCommand(
    string Code,
    string Description,
    ProcedureCategory Category,
    decimal DefaultFee,
    string Currency,
    int DefaultDurationMinutes) : ICommand<ProcedureDto>;
