using Microsoft.AspNetCore.Authorization;

namespace Norse.Abstractions.Components.Authorization;

/// <summary>
///     Requires a fresh <see cref="NorseClaimTypes.AuthTime" />: present, parseable, not in the future, and no older
///     than the configured window. Midgard's handler owns the window and fails with a distinguishable reason that
///     its pipeline maps to the re-authentication hop.
/// </summary>
public sealed record RecentAuthenticationRequirement : IAuthorizationRequirement;
