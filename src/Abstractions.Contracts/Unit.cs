using System.Diagnostics.CodeAnalysis;

namespace Norse.Abstractions.Contracts;

/// <summary>
///     The payload of <see cref="Outcome{T}" /> for operations with no success value — not a placeholder,
///     the honest zero-cost type for "nothing." Trivially satisfies <c>where T : notnull</c> (structs
///     always do). Spelled bare as <c>Outcome</c> almost everywhere via the platform-wide alias in
///     <c>GlobalUsings.Outcome.cs</c> — most call sites never name <see cref="Unit" /> directly.
/// </summary>
public readonly record struct Unit
{
	/// <summary>The single value of this type.</summary>
	[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily",
		Justification = "Unit has no meaningful state; the static field documents the single inhabited value.")]
	public static readonly Unit Value = default;
}
