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
}
