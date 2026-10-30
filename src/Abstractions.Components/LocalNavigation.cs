using System.Diagnostics.CodeAnalysis;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Components;

/// <summary>
///     The shared check a client runs before it navigates to a server-authored URL: only a single-slash-rooted
///     local path passes, so a value that reached the client cannot turn it into an open redirect.
/// </summary>
public static class LocalNavigation
{
	const int MaxLength = 2048;

	/// <summary>
	///     True when <paramref name="url" /> is non-empty, at most 2048 characters, free of control characters (C0,
	///     DEL and C1), spaces and backslashes, and starts with exactly one slash, and all of that still holds after
	///     one percent-decode (<c>/%2F/evil.example</c> decodes to <c>//evil.example</c>).
	/// </summary>
	/// <param name="url">The candidate URL.</param>
	public static bool IsLocal([NotNullWhen(true)] string? url) =>
		url is { Length: > 0 and <= MaxLength }
		&& IsSingleSlashRooted(url)
		&& IsSingleSlashRooted(Uri.UnescapeDataString(url));

	// The one navigation predicate both component bases share: a reauthentication refusal whose next hop is
	// local. Every other failure, a NextUrl included, renders as a problem.
	internal static bool IsReauthenticationLanding(Problem problem, [NotNullWhen(true)] out string? nextUrl)
	{
		nextUrl = problem.NextUrl;
		return problem is { Category: ErrorCategory.Forbidden, Code: NorseErrorCodes.ReauthenticationRequired }
			&& IsLocal(nextUrl);
	}

	// Controls (C0, DEL and C1, as Midgard's /_auth/complete refuses them) and space: a browser strips tab and
	// newline while parsing, so "/\t/evil.example" would otherwise resolve as "//evil.example". A backslash is
	// refused anywhere, since a browser reads it as a slash.
	static bool IsSingleSlashRooted(string url) =>
		url is ['/', ..] and not ['/', '/', ..]
		&& !url.Any(static c => char.IsControl(c) || c is ' ' or '\\');
}
