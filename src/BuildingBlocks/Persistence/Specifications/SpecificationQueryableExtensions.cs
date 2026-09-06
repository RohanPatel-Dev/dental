namespace Dental.Framework.Persistence.Specifications;

/// <summary>Fluent entry point for applying a specification.</summary>
public static class SpecificationQueryableExtensions
{
    /// <summary>Applies a specification to the query.</summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>The composed query.</returns>
    public static IQueryable<T> WithSpecification<T>(
        this IQueryable<T> query,
        Specification<T> specification)
        where T : class =>
        SpecificationEvaluator.GetQuery(query, specification);
}
