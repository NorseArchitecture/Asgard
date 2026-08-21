using Microsoft.AspNetCore.Authorization;

namespace Norse.Abstractions.Web.Server.Authorization;

/// <summary>Declares the platform-standard authorization policies.</summary>
public static class NorsePlatformPolicies
{
	/// <summary>Any principal, the anonymous role included.</summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(NorsePolicies.Anonymous)]
	public static void Anonymous(AuthorizationPolicyBuilder policy) =>
		policy.RequireAuthenticatedUser();

	/// <summary>The orchestrator-probe lane.</summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(NorsePolicies.Probe)]
	public static void Probe(AuthorizationPolicyBuilder policy) =>
		// Deliberately an always-succeed assertion rather than nothing: AuthorizationPolicy's constructor
		// throws on an empty requirement set, so "requires nothing" has to be spelled. It is also the honest
		// shape -- an orchestrator probe carries no principal at all, so RequireAuthenticatedUser would be
		// wrong, not merely stricter. This is the one place the RequireAssertion(_ => true) pattern the rest
		// of this train deletes is actually correct.
		policy.RequireAssertion(_ => true);
}
