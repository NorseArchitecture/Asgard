namespace Norse.Abstractions.Components.Authorization;

/// <summary>Platform-standard claim types every realm can rely on.</summary>
public static class NorseClaimTypes
{
	/// <summary>
	///     The time the principal last presented a credential, in integral epoch seconds. Stamped on credential
	///     sign-in, carried unchanged across cookie refreshes and every token minted from the grant; never PII.
	/// </summary>
	public const string AuthTime = "auth_time";
}
