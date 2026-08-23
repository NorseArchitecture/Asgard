namespace Norse.Abstractions.Components.Authorization;

/// <summary>
///     Platform-standard policy names — the seed of Asgard#57's standard set. Realm-specific names stay in
///     their own <c>{Context}Policies</c> classes; only names every realm can rely on live here.
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
	///     <i>authentication</i> lane keeps them out of the browser composite.
	/// </summary>
	public const string Probe = "Norse.Probe";

	/// <summary>
	///     Every REST facade controller (from <c>Abstractions.Web.Server.Facade.GrpcControllerBase</c>),
	///     platform-wide, by construction (class-level <c>[Authorize]</c>). Satisfied only by
	///     a bearer JWT, never the browser cookie — Midgard's <c>NorseLaneSelector</c> forwards every
	///     facade endpoint to <c>NorseSchemes.Machine</c> structurally, which is what actually keeps a
	///     cookie principal out; this policy only checks that a principal exists, matching
	///     <see cref="Anonymous" />/<see cref="Probe" /> exactly.
	/// </summary>
	public const string Machine = "Norse.Machine";
}
