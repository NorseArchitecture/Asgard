namespace Norse.Abstractions.Contracts;

/// <summary>
/// Application-level error vocabulary an <see cref="Outcome{T}"/> carries on failure.
/// <see cref="LockedOut"/>/<see cref="InvalidCredentials"/>/<see cref="NotAllowed"/> are an
/// AuthN-specific extension over the platform's base Validation/NotFound/Conflict trio.
/// <see cref="Unauthorized"/>/<see cref="Forbidden"/> split not-authenticated from
/// authenticated-but-lacks-the-policy — every request carries a principal (anonymous role included),
/// so both are live, reachable paths. <see cref="Fault"/> is the catch-all for anything unmapped.
/// </summary>
public enum ErrorCategory : byte
{
	/// <summary>Request shape or field-level validation failure.</summary>
	Validation = 1,
	/// <summary>Resource not found.</summary>
	NotFound = 2,
	/// <summary>Conflict with existing state.</summary>
	Conflict = 3,
	/// <summary>Account or resource is locked out.</summary>
	LockedOut = 4,
	/// <summary>Invalid credentials provided. Vestigial — not actively produced, per the anti-enumeration ruling.</summary>
	InvalidCredentials = 5,
	/// <summary>Operation not allowed given current state (a precondition failure, not an authorization failure).</summary>
	NotAllowed = 6,
	/// <summary>Caller is not authenticated for an operation that requires it.</summary>
	Unauthorized = 7,
	/// <summary>Caller is authenticated but lacks the required policy.</summary>
	Forbidden = 8,
	/// <summary>Unmapped failure. Always carries a <see cref="Problem.CorrelationId"/>.</summary>
	Fault = 9
}
