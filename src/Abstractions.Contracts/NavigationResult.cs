using System.Runtime.Serialization;

namespace Norse.Abstractions.Contracts;

/// <summary>
///     The next-hop response — success for any operation whose meaning is "you're done here; go
///     there." Think of a wizard: a form presents a question, and the answer governs the next
///     step — only the server knows the map, so the URL is always server-resolved and concrete,
///     and the client navigates it unconditionally: no flag to branch on, no route to build, no
///     default of its own to apply. First consumers are the gate's issuance operations
///     (Login/Register/Logout — a 2FA challenge and a deferred-completion URL ride success as
///     ordinary hops), but the shape carries no domain opinion, which is why it lives here with
///     <see cref="BoolResponse" /> and <see cref="Unit" /> rather than in any one realm.
/// </summary>
[DataContract]
public sealed record NavigationResult
{
	/// <summary>The server-resolved next hop.</summary>
	[DataMember(Order = 1)]
	public required string NextUrl { get; init; }
}
