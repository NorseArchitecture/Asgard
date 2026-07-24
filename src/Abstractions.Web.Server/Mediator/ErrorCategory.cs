namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// Trimmed application-level error vocabulary an <see cref="Outcome"/>/<see cref="Outcome{T}"/> carries
/// on failure. <see cref="LockedOut"/>/<see cref="InvalidCredentials"/>/<see cref="NotAllowed"/> are an
/// AuthN-specific extension over the platform's base Validation/NotFound/Conflict trio — the first real
/// consumer of this type, per <c>Heimdall/specs/2026-07-13-authn-identity-split-design.md</c> §3.1.
/// </summary>
public enum ErrorCategory : byte
{
	/// <summary>Validation failure.</summary>
	Validation = 1,
	/// <summary>Resource not found.</summary>
	NotFound = 2,
	/// <summary>Conflict with existing state.</summary>
	Conflict = 3,
	/// <summary>Account or resource is locked out.</summary>
	LockedOut = 4,
	/// <summary>Invalid credentials provided.</summary>
	InvalidCredentials = 5,
	/// <summary>Operation not allowed.</summary>
	NotAllowed = 6
}
