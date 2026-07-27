using System.Security.Claims;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// The pipeline's ambient principal source — the Bogard scoped-context pattern, typed (spec §2.4).
/// Each channel adapter supplies it at entry: Midgard's gRPC seeding interceptor stamps the request
/// principal; inside a circuit the implementation defers to <c>AuthenticationStateProvider</c> live
/// (a circuit outlives login/logout, so an eagerly seeded value would go stale). Resolving a
/// principal in a scope no channel adapter prepared fails loudly — never a silent anonymous.
/// </summary>
public interface IPrincipalAccessor
{
	/// <summary>Gets the current caller's principal.</summary>
	ValueTask<ClaimsPrincipal> GetPrincipalAsync(CancellationToken cancellationToken = default);
}
