namespace Norse.Abstractions.Contracts;

/// <summary>The platform's <see cref="Problem.Code" /> values.</summary>
public static class NorseErrorCodes
{
	/// <summary>
	///     The caller is signed in, but not recently enough for the operation: <see cref="ErrorCategory.Forbidden" />,
	///     with <see cref="Problem.NextUrl" /> naming the sign-in that refreshes the authentication.
	/// </summary>
	public const string ReauthenticationRequired = "reauthentication-required";
}
