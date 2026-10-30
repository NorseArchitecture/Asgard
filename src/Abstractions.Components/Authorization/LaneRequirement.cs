using Microsoft.AspNetCore.Authorization;

namespace Norse.Abstractions.Components.Authorization;

/// <summary>
///     Requires the principal to have been authenticated by one of <paramref name="Lanes" />. Midgard's handler
///     reads the lane from the stamped authentication type on the server, and treats the client's principal as
///     <see cref="NorseLanes.Identity" /> on the client.
/// </summary>
/// <remarks>
///     A lane matches only on an authenticated identity whose authentication type the platform stamped. An
///     unauthenticated identity never matches any lane, so the requirement also demands a principal; policies
///     such as <see cref="NorsePolicies.Machine" /> rely on that instead of <c>RequireAuthenticatedUser</c>.
/// </remarks>
/// <param name="Lanes">The admitted lanes, from <see cref="NorseLanes" />.</param>
public sealed record LaneRequirement(IReadOnlyList<string> Lanes) : IAuthorizationRequirement;
