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
}
