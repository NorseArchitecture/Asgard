using Microsoft.AspNetCore.Authorization;

namespace Norse.Abstractions.Components.Authorization;

/// <summary>Requires an <c>aud</c> claim equal to <paramref name="Audience" />.</summary>
/// <param name="Audience">The required audience, from <see cref="NorseAudiences" />.</param>
/// <param name="Lane">
///     The lane the requirement applies to, from <see cref="NorseLanes" />; a principal from any other lane
///     passes it untouched. <see langword="null" /> applies it to every lane.
/// </param>
public sealed record AudienceRequirement(string Audience, string? Lane) : IAuthorizationRequirement;
