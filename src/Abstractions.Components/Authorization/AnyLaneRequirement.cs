using Microsoft.AspNetCore.Authorization;

namespace Norse.Abstractions.Components.Authorization;

/// <summary>
///     The explicit, greppable opt-out from the default lane closure: imposes no lane restriction at all.
/// </summary>
/// <remarks>
///     Its handlers succeed it for every principal, an empty one included, because a probe authenticates
///     nothing. Its presence only stops Midgard's generated registration from closing the policy to
///     <see cref="NorseLanes.Identity" />; the policy's other requirements still decide.
/// </remarks>
public sealed record AnyLaneRequirement : IAuthorizationRequirement;
