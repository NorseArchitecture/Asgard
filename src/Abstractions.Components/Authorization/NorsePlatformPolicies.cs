using Microsoft.AspNetCore.Authorization;

namespace Norse.Abstractions.Components.Authorization;

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
		// of this train deletes is actually correct. AllowAnyLane keeps the default closure off a policy
		// whose caller has no lane to be closed to.
		policy
			.AllowAnyLane()
			.RequireAssertion(_ => true);

	/// <summary>Every REST facade controller. Scheme routing is Midgard's lane selector's job, not this policy's.</summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(NorsePolicies.Machine)]
	public static void Machine(AuthorizationPolicyBuilder policy) =>
		policy
			.RequireLane(NorseLanes.Machine)
			.RequireAudience(NorseAudiences.Facade);

	/// <summary>The freshness gate, stacked on an operation's own policy.</summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(NorsePolicies.RecentAuthentication)]
	public static void RecentAuthentication(AuthorizationPolicyBuilder policy) =>
		// AllowAnyLane, never a lane of its own: the operation's other policy decides the admitted lanes, and
		// this one must never narrow them.
		policy
			.AllowAnyLane()
			.AddRequirements(new RecentAuthenticationRequirement());
}
