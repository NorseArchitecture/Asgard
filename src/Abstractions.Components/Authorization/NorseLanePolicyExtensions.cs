using Microsoft.AspNetCore.Authorization;

namespace Norse.Abstractions.Components.Authorization;

/// <summary>Lane, audience and default-closure helpers for <see cref="AuthorizationPolicyBuilder" />.</summary>
public static class NorseLanePolicyExtensions
{
	extension(AuthorizationPolicyBuilder policy)
	{
		/// <summary>Admits only principals authenticated by one of <paramref name="lanes" />.</summary>
		/// <param name="lanes">The admitted lanes, from <see cref="NorseLanes" />; at least one.</param>
		/// <returns>The builder, for chaining.</returns>
		/// <exception cref="ArgumentException">
		///     <paramref name="lanes" /> is empty, or one of its entries is null, empty or whitespace.
		/// </exception>
		public AuthorizationPolicyBuilder RequireLane(params string[] lanes)
		{
			ArgumentNullException.ThrowIfNull(lanes);
			if (lanes.Length is 0 || lanes.Any(string.IsNullOrWhiteSpace))
				throw new ArgumentException(
					"At least one lane is required, and every lane must be named.", nameof(lanes));

			return policy.AddRequirements(new LaneRequirement([.. lanes]));
		}

		/// <summary>Opts the policy out of the default lane closure; see <see cref="AnyLaneRequirement" />.</summary>
		/// <returns>The builder, for chaining.</returns>
		public AuthorizationPolicyBuilder AllowAnyLane() =>
			policy.AddRequirements(new AnyLaneRequirement());

		/// <summary>Requires an <c>aud</c> claim equal to <paramref name="audience" />.</summary>
		/// <param name="audience">The required audience, from <see cref="NorseAudiences" />.</param>
		/// <param name="lane">The lane the check applies to; <see langword="null" /> applies it to every lane.</param>
		/// <returns>The builder, for chaining.</returns>
		/// <exception cref="ArgumentException">
		///     <paramref name="audience" /> is null, empty or whitespace, or <paramref name="lane" /> is empty or
		///     whitespace: a blank lane would match no real lane and silently disable the check.
		/// </exception>
		public AuthorizationPolicyBuilder RequireAudience(string audience, string? lane = null)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(audience);
			if (lane is not null)
				ArgumentException.ThrowIfNullOrWhiteSpace(lane);

			return policy.AddRequirements(new AudienceRequirement(audience, lane));
		}

		/// <summary>
		///     Closes the policy to <see cref="NorseLanes.Identity" /> unless it already names its lanes with
		///     <see cref="RequireLane" /> or opts out with <see cref="AllowAnyLane" />. Midgard's generated
		///     registration calls it after every declared policy's configure method.
		/// </summary>
		/// <returns>The builder, for chaining.</returns>
		public AuthorizationPolicyBuilder CloseLaneByDefault() =>
			policy.Requirements.Any(r => r is LaneRequirement or AnyLaneRequirement) ?
				policy :
				policy.RequireLane(NorseLanes.Identity);
	}
}
