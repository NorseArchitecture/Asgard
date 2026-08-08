using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Norse.Abstractions.Web.Server.DeferredSignIn;

/// <summary>
///     Stashes a sign-in or sign-out that cannot complete on the current request (an already-established
///     Blazor Server interactive circuit, where <c>HttpContext.Response.HasStarted</c> is already true) so
///     it can be completed on a genuine, later HTTP request instead. Zero domain knowledge — reusable by
///     any future realm hosting cookie-based auth behind an interactive Blazor Server component.
/// </summary>
public interface IDeferredSignIn
{
	/// <summary>Stashes a pending sign-in. Returns a one-time completion key.</summary>
	string StashSignIn(string scheme, ClaimsPrincipal principal, AuthenticationProperties properties);

	/// <summary>Stashes a pending sign-out. Returns a one-time completion key.</summary>
	string StashSignOut(string scheme);

	/// <summary>Consumes (and removes) a completion key. Returns false if the key is unknown or expired.</summary>
	bool TryConsume(string key, out DeferredSignInAction action);

	/// <summary>
	///     Builds the URL a caller force-navigates to in order to complete a deferred sign-in/out for
	///     <paramref name="key" />, landing on <paramref name="returnUrl" /> afterward. The completion route
	///     pattern itself is an implementation detail (Midgard's <c>DeferredSignInEndpointRouteBuilderExtensions</c>)
	///     deliberately not exposed here — a caller building this URL (e.g. a service translating a stashed
	///     key into a response field) has no legitimate reason to know the pattern, only the finished URL.
	/// </summary>
	string BuildCompletionUrl(string key, string returnUrl);
}

/// <summary>What to do to complete a deferred sign-in/out. <see cref="Principal" /> is null for sign-out.</summary>
/// <param name="Scheme">The authentication scheme to complete the sign-in or sign-out against.</param>
/// <param name="SignOut">True when this action completes a sign-out; false when it completes a sign-in.</param>
/// <param name="Principal">The principal to sign in. Null for sign-out.</param>
/// <param name="Properties">The authentication properties to apply. Null for sign-out.</param>
public sealed record DeferredSignInAction(
	string Scheme,
	bool SignOut,
	ClaimsPrincipal? Principal,
	AuthenticationProperties? Properties);
