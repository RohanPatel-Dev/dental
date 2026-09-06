using System.Linq.Expressions;

namespace Dental.Framework.Persistence.Specifications;

/// <summary>
/// Composable query definition: filters, includes, ordering and paging in one object.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <remarks>
/// <see cref="AsNoTracking"/> defaults to <see langword="true"/>. Turn it OFF for a read-then-mutate
/// flow: an untracked entity will not be saved.
/// </remarks>
public abstract class Specification<T>
    where T : class
{
    /// <summary>Predicates ANDed together.</summary>
    public IList<Expression<Func<T, bool>>> Criteria { get; } = [];

    /// <summary>Navigation properties to eager load.</summary>
    public IList<Expression<Func<T, object>>> Includes { get; } = [];

    /// <summary>String based includes, for nested paths.</summary>
    public IList<string> IncludeStrings { get; } = [];

    /// <summary>Primary ascending sort.</summary>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <summary>Primary descending sort.</summary>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <summary>Rows to skip, when paging is enabled.</summary>
    public int Skip { get; private set; }

    /// <summary>Rows to take, when paging is enabled.</summary>
    public int Take { get; private set; }

    /// <summary>True once <see cref="ApplyPaging"/> has been called.</summary>
    public bool IsPagingEnabled { get; private set; }

    /// <summary>Whether the query runs without change tracking. Defaults to <see langword="true"/>.</summary>
    public bool AsNoTracking { get; protected set; } = true;

    /// <summary>Whether to drop the soft delete filter and include deleted rows.</summary>
    public bool IncludeSoftDeleted { get; protected set; }

    /// <summary>Adds a predicate.</summary>
    /// <param name="criteria">The predicate.</param>
    protected void Where(Expression<Func<T, bool>> criteria) => Criteria.Add(criteria);

    /// <summary>Adds an eager load.</summary>
    /// <param name="include">The navigation to include.</param>
    protected void Include(Expression<Func<T, object>> include) => Includes.Add(include);

    /// <summary>Adds a string based eager load, for nested paths.</summary>
    /// <param name="includeString">The include path.</param>
    protected void Include(string includeString) => IncludeStrings.Add(includeString);

    /// <summary>Sets the ascending sort.</summary>
    /// <param name="orderBy">Sort selector.</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;

    /// <summary>Sets the descending sort.</summary>
    /// <param name="orderByDescending">Sort selector.</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescending) =>
        OrderByDescending = orderByDescending;

    /// <summary>Enables paging.</summary>
    /// <param name="skip">Rows to skip.</param>
    /// <param name="take">Rows to take.</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>Turns change tracking back on, for a read-then-mutate flow.</summary>
    protected void EnableTracking() => AsNoTracking = false;
}
