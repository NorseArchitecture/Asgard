using Norse.Abstractions.Components;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
///     Names the local page a request lands back on after a reauthentication: when the request is refused as
///     <see cref="Contracts.NorseErrorCodes.ReauthenticationRequired" />, the server builds the problem's
///     <see cref="Contracts.Problem.NextUrl" /> as a sign-in whose return URL is this path. Server-only, beside the
///     mediator marker family, so the wire record never names it.
/// </summary>
/// <param name="path">The path-base-relative landing, for example <c>/Account/Manage/Passkeys</c>.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ReauthenticationLandingAttribute(string path) : Attribute
{
	/// <summary>The path-base-relative landing.</summary>
	/// <exception cref="ArgumentException">The path is not local by <see cref="LocalNavigation.IsLocal" />.</exception>
	public string Path { get; } = LocalNavigation.IsLocal(path) ?
		path :
		throw new ArgumentException("A reauthentication landing must be a single-slash-rooted local path.", nameof(path));
}
