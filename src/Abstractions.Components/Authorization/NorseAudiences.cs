namespace Norse.Abstractions.Components.Authorization;

/// <summary>
///     The platform's bearer audiences. One validation scheme accepts both, so every bearer-admitting policy pins
///     its audience with <see cref="NorseLanePolicyExtensions.RequireAudience" />. A scheme may share an
///     audience's string, but the two are separate constants with separate checks.
/// </summary>
public static class NorseAudiences
{
	/// <summary>The machine audience: every REST facade controller.</summary>
	public const string Facade = "Norse.Facade";

	/// <summary>The user audience: access tokens minted for a signed-in user.</summary>
	public const string User = "Norse.User";
}
