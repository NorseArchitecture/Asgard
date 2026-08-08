namespace Norse.Abstractions.Contracts;

/// <summary>
///     Application-level error vocabulary an <see cref="Outcome{T}" /> carries on failure.
///     <see cref="LockedOut" />/<see cref="InvalidCredentials" />/<see cref="NotAllowed" /> are an
///     AuthN-specific extension over the platform's base Validation/NotFound/Conflict trio.
///     <see cref="Unauthorized" />/<see cref="Forbidden" /> split not-authenticated from
///     authenticated-but-lacks-the-policy — every request carries a principal (anonymous role included),
///     so both are live, reachable paths. <see cref="Fault" /> is the catch-all for anything unmapped.
/// </summary>
public enum ErrorCategory : byte
{
	/// <summary>Sentinel CLR default — never a valid category; a failure always names its cause.</summary>
	Unspecified = 0,

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

	/// <summary>Unmapped failure. Always carries a <see cref="Problem.CorrelationId" />.</summary>
	Fault = 9,

	/// <summary>
	///     A Single-cardinality read asserted exactly one match and the data returned more — an expected
	///     domain state on the wire, but a data-integrity smell worth telemetry even when handled
	///     (well-and-wire spec §3.2). Not a peer of <see cref="NotFound" /> in severity.
	/// </summary>
	MultipleMatches = 10,

	/// <summary>
	///     Intentionally gone: the record existed, the content was deliberately retired — the system
	///     working as designed, neither <see cref="NotFound" /> nor an incident. Producer-agnostic:
	///     crypto-shredding (per-subject key destroyed; <see cref="Problem.Receipt" /> populated) and
	///     content tombstoning (retired into temporal history; no receipt) both answer with this
	///     category, and both fold to 410 Gone at the REST edge.
	/// </summary>
	Erased = 11
}
