namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// The mediator's trivial "did it work" success payload — used by handlers whose only meaningful success
/// signal is a boolean (e.g. a credential check that can legitimately succeed with a false result, not an
/// error). Server-only, never serialized directly; see <see cref="Outcome{T}"/>.
/// </summary>
public sealed record BoolResponse
{
	/// <summary>
	/// The boolean success payload.
	/// </summary>
	public required bool Value { get; init; }
}
