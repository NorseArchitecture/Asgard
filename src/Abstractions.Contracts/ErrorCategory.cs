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

	/// <summary>
	///     Invalid credentials provided. Deliberately generic — the anti-enumeration stance means a
	///     login rejection never discloses which credential failed; Himinbjörg's <c>LoginHandler</c>
	///     produces exactly this category with one shared message ("Invalid email or password.").
	///     (Ruled 2026-08-08: a prior claim here that this member was vestigial was the stale side of
	///     a docs-vs-code drift — the working code stands.)
	/// </summary>
	InvalidCredentials = 5,

	/// <summary>
	///     The caller may not perform this operation in the current state — an authorization answer, not a
	///     request-shape one. Folds to 403 (spec §1.8, ruled 2026-08-21): the question it answers is
	///     "can I do the thing?", never "is this well-formed?". Its prior contract named it a precondition
	///     failure folding to 400; that reading was amended rather than left to contradict the mapping.
	///     Sole production producer is Himinbjörg's <c>LoginHandler</c> for <c>SignInResult.IsNotAllowed</c>.
	/// </summary>
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
