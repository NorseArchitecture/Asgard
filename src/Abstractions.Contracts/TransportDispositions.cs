namespace Norse.Abstractions.Contracts;

/// <summary>
///     The one declaration both transport edges project from — Asgard's <c>GrpcControllerBase.FoldAsync</c>
///     and Midgard's <c>ProblemExtensions.ToRpcException</c>. Before this table the two were hand-written
///     switch statements whose agreement rested on a doc comment; they can no longer disagree because
///     there is only one of them.
/// </summary>
public static class TransportDispositions
{
	/// <summary>Resolves the declared transport shape for <paramref name="category" />.</summary>
	/// <remarks>
	///     Deliberately a switch expression with <b>no default arm</b>: adding an <see cref="ErrorCategory" />
	///     member without declaring its disposition is CS8509, which is an error under the platform's
	///     warnings-as-errors posture. Compile time, not test time. CS8524 (exhaustiveness over the
	///     underlying byte type) is suppressed because an unnamed byte value cannot occur in normal code —
	///     the switch reaches all defined <see cref="ErrorCategory" /> members, which is what matters;
	///     a new named member left unmapped still fails the build with CS8509.
	/// </remarks>
	// CS8524 deliberately suppressed: this switch is exhaustive over every named ErrorCategory member.
	// An unnamed byte value (0-255 excluding defined members) cannot occur in normal code. Suppressing
	// (rather than adding a default arm) preserves the guarantee: a NEW named member left unmapped must
	// fail the BUILD (CS8509), not silently compile and throw at runtime.
#pragma warning disable CS8524
	public static TransportDisposition For(ErrorCategory category) =>
		category switch
		{
			ErrorCategory.Validation => new(400, 3, true),
			ErrorCategory.NotFound => new(404, 5, false),
			ErrorCategory.Conflict => new(409, 6, true),
			ErrorCategory.LockedOut => new(403, 7, true),
			ErrorCategory.InvalidCredentials => new(401, 16, false),
			ErrorCategory.NotAllowed => new(403, 7, true),
			ErrorCategory.Unauthorized => new(401, 16, false),
			ErrorCategory.Forbidden => new(403, 7, true),
			ErrorCategory.Fault => new(500, 13, true),
			ErrorCategory.MultipleMatches => new(500, 13, true),
			ErrorCategory.Erased => new(410, 5, true),
			ErrorCategory.Unspecified => new(500, 2, false)
		};
#pragma warning restore CS8524
}
