using System.Linq.Expressions;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Backend;

/// <summary>
///     The consumer-side read contract for a well (well-and-wire spec §3.2). Every member returns
///     <see cref="Outcome{T}" />; <c>Succeeded ⇒ value is present</c> — absence is a
///     <see cref="Problem" /> (<see cref="ErrorCategory.NotFound" />), never a null. Consumption is
///     <see cref="Outcome{T}.Match{TResult}" />: acceptability of absence is a call-site judgment,
///     never marshalled into the envelope. <typeparamref name="TView" /> is deliberately unconstrained —
///     the entity-side <see cref="IViewBearer{TView}" /> constraint belongs to the implementation.
/// </summary>
/// <typeparam name="TView">The well's read-model document type.</typeparam>
public interface IReadRepository<TView> where TView : notnull
{
	/// <summary>
	///     Identity path — filters on the root PK internally; no caller-expressed predicate, so the scan cannot be
	///     written by accident. Absence → <see cref="ErrorCategory.NotFound" />.
	/// </summary>
	Task<Outcome<TView>> GetAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>Identity path with SQL-side projection.</summary>
	Task<Outcome<TProjection>> GetAsync<TProjection>(Guid id, Expression<Func<TView, TProjection>> projection,
		CancellationToken cancellationToken = default)
		where TProjection : notnull;

	/// <summary>
	///     Asserts exactly one match: 0 → <see cref="ErrorCategory.NotFound" />; more →
	///     <see cref="ErrorCategory.MultipleMatches" />.
	/// </summary>
	Task<Outcome<TView>> SingleAsync(Expression<Func<TView, bool>> predicate,
		CancellationToken cancellationToken = default);

	/// <summary>Asserts exactly one match, with SQL-side projection.</summary>
	Task<Outcome<TProjection>> SingleAsync<TProjection>(Expression<Func<TView, bool>> predicate,
		Expression<Func<TView, TProjection>> projection, CancellationToken cancellationToken = default)
		where TProjection : notnull;

	/// <summary>Asserts at least one match: 0 → <see cref="ErrorCategory.NotFound" />.</summary>
	Task<Outcome<TView>> FirstAsync(Expression<Func<TView, bool>> predicate,
		CancellationToken cancellationToken = default);

	/// <summary>Asserts at least one match, with SQL-side projection.</summary>
	Task<Outcome<TProjection>> FirstAsync<TProjection>(Expression<Func<TView, bool>> predicate,
		Expression<Func<TView, TProjection>> projection, CancellationToken cancellationToken = default)
		where TProjection : notnull;

	/// <summary>Set query — asserts nothing, so emptiness is data: an empty list is a value, never a <see cref="Problem" />.</summary>
	Task<Outcome<IReadOnlyList<TView>>> ListAsync(Expression<Func<TView, bool>> predicate,
		CancellationToken cancellationToken = default);

	/// <summary>Set query with SQL-side projection.</summary>
	Task<Outcome<IReadOnlyList<TProjection>>> ListAsync<TProjection>(Expression<Func<TView, bool>> predicate,
		Expression<Func<TView, TProjection>> projection, CancellationToken cancellationToken = default)
		where TProjection : notnull;
}
