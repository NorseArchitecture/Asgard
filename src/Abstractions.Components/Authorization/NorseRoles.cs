namespace Norse.Abstractions.Components.Authorization;

/// <summary>
///     Platform-standard role names — only names every realm can rely on live here. Realm-specific roles
///     stay in their own realm.
/// </summary>
public static class NorseRoles
{
	/// <summary>
	///     The role carried by the anonymous principal. Midgard's <c>NorseAnonymousHandler</c> mints exactly
	///     this role for a request with no credentials, and policies that must exclude the anonymous
	///     principal do so by this name — one literal, declared once, so the minting side and the excluding
	///     side cannot drift apart.
	/// </summary>
	public const string Anonymous = "anonymous";
}
