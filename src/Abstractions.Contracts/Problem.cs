namespace Norse.Abstractions.Contracts;

/// <summary>
/// The structured detail an <see cref="Outcome{T}"/> carries on failure.
/// </summary>
public sealed record Problem
{
	/// <summary>The error category.</summary>
	public required ErrorCategory Category { get; init; }

	/// <summary>Field-keyed validation or structured errors.</summary>
	public IReadOnlyDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();

	/// <summary>
	/// Populated only for <see cref="ErrorCategory.Fault"/> — every other category is deterministic and
	/// reproducible from the request itself, so a trace handle adds no diagnostic value there.
	/// </summary>
	public Guid? CorrelationId { get; init; }

	/// <summary>
	/// The erasure proof, populated only when <see cref="Category"/> is
	/// <see cref="ErrorCategory.Erased"/> and a ledger entry exists (crypto-shred producer);
	/// <see langword="null"/> for tombstone producers and every other category.
	/// </summary>
	public ErasureReceipt? Receipt { get; init; }

	/// <summary>
	/// Creates a <see cref="Problem"/> carrying one model-level message — the empty-string key both
	/// Blazor and FluentValidation reserve for errors not tied to any field — so call sites never
	/// hand-build the single-entry dictionary literal.
	/// </summary>
	/// <param name="category">The error category the message belongs to.</param>
	/// <param name="message">The model-level message.</param>
	public static Problem ModelError(ErrorCategory category, string message) =>
		new()
		{
			Category = category,
			Errors = new Dictionary<string, string[]> { [string.Empty] = [message] },
		};
}
