using Dental.Modules.Clinical.Contracts.v1.ChartEntries.GetToothChart;
using FluentValidation;

namespace Dental.Modules.Clinical.Features.v1.ChartEntries.GetToothChart;

/// <summary>Validates <see cref="GetToothChartQuery"/>.</summary>
public sealed class GetToothChartQueryValidator : AbstractValidator<GetToothChartQuery>
{
    /// <summary>Builds the rules.</summary>
    public GetToothChartQueryValidator() => RuleFor(q => q.PatientId).NotEmpty();
}
