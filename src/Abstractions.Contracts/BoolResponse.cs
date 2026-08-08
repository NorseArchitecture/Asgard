using System.Runtime.Serialization;

namespace Norse.Abstractions.Contracts;

/// <summary>
///     The mediator's trivial "did it work" success payload — used by handlers whose only meaningful success
///     signal is a boolean (e.g. a credential check that can legitimately succeed with a false result, not an
///     error). Serialized directly over the wire — e.g. <c>IAuthenticationService.EmailExists</c>'s
///     <c>Outcome&lt;BoolResponse&gt;</c> response — so it carries the same explicit <see cref="DataContractAttribute" />/
///     <see cref="DataMemberAttribute" /> shape as its sibling wire response types (e.g. <c>LoginResult</c>); see
///     <see cref="Outcome{T}" />.
/// </summary>
[DataContract]
public sealed record BoolResponse
{
	/// <summary>
	///     The boolean success payload.
	/// </summary>
	[DataMember(Order = 1)]
	public required bool Value { get; init; }
}
