namespace Norse.Abstractions.Web.Server.Authorization;

/// <summary>
///     Platform-standard policy names — the seed of Asgard#57's standard set. Realm-specific names stay in
///     their own <c>{Context}Policies</c> classes; only names every realm can rely on live here.
///     <c>Machine</c> is deliberately absent: it arrives with Himinbjorg#49, declared through
///     <see cref="NorsePolicyAttribute" /> rather than beside it.
/// </summary>
public static class NorsePolicies
{
	/// <summary>
	///     Satisfied by any principal, the anonymous role included. Every request carries a principal, so
	///     this is a real requirement (<c>RequireAuthenticatedUser</c>) rather than the
	///     <c>RequireAssertion(_ =&gt; true)</c> placeholder it replaces.
	/// </summary>
	public const string Anonymous = "Norse.Anonymous";

	/// <summary>
	///     The orchestrator-probe lane: liveness and readiness. Requires nothing, and that is the point —
	///     the exemption is named, greppable, and reviewable instead of an <c>AllowAnonymous</c> escape
	///     hatch NORSE013 would strike. Probe endpoints never reach the mediator, and the probe
	///     <i>authentication</i> lane (Task 7) keeps them out of the browser composite.
	/// </summary>
	public const string Probe = "Norse.Probe";
}
