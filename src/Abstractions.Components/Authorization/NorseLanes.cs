namespace Norse.Abstractions.Components.Authorization;

/// <summary>
///     The platform's authentication lanes. Midgard stamps the lane that produced a principal as its
///     <see cref="System.Security.Claims.ClaimsIdentity.AuthenticationType" />; nothing a token or cookie carries can
///     set it. Declared here, client-safe, so every realm's policy classes can name them.
/// </summary>
public static class NorseLanes
{
	/// <summary>The identity cookie, the anonymous cookie included.</summary>
	public const string Identity = "Norse.Lane.Identity";

	/// <summary>A user bearer token, minted by the authorization code flow.</summary>
	public const string User = "Norse.Lane.User";

	/// <summary>A machine bearer token, minted by the client credentials flow for the REST facade.</summary>
	public const string Machine = "Norse.Lane.Machine";
}
