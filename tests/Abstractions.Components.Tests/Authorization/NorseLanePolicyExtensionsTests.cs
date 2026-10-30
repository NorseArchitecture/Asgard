using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Components.Authorization;

namespace Norse.Abstractions.Components.Tests.Authorization;

public sealed class NorseLanePolicyExtensionsTests
{
	[Fact]
	void The_lane_audience_and_claim_names_are_the_declared_literals()
	{
		NorseLanes.Identity.ShouldBe("Norse.Lane.Identity");
		NorseLanes.User.ShouldBe("Norse.Lane.User");
		NorseLanes.Machine.ShouldBe("Norse.Lane.Machine");
		NorseAudiences.Facade.ShouldBe("Norse.Facade");
		NorseAudiences.User.ShouldBe("Norse.User");
		NorseClaimTypes.AuthTime.ShouldBe("auth_time");
	}

	[Fact]
	void Close_lane_by_default_adds_identity_when_none_declared()
	{
		AuthorizationPolicyBuilder policy = new();

		policy
			.RequireAuthenticatedUser()
			.CloseLaneByDefault();

		policy.Requirements.OfType<LaneRequirement>().Single().Lanes.ShouldBe([NorseLanes.Identity]);
	}

	[Fact]
	void Close_lane_by_default_leaves_require_lane_alone()
	{
		AuthorizationPolicyBuilder policy = new();

		policy
			.RequireLane(NorseLanes.Identity, NorseLanes.User)
			.CloseLaneByDefault();

		policy.Requirements.OfType<LaneRequirement>().Single().Lanes
			.ShouldBe([NorseLanes.Identity, NorseLanes.User]);
	}

	[Fact]
	void Close_lane_by_default_leaves_allow_any_lane_alone()
	{
		AuthorizationPolicyBuilder policy = new();

		policy
			.AllowAnyLane()
			.CloseLaneByDefault();

		policy.Requirements.ShouldHaveSingleItem().ShouldBeOfType<AnyLaneRequirement>();
	}

	[Fact]
	void Require_lane_refuses_an_empty_lane_set() =>
		Should.Throw<ArgumentException>(() => new AuthorizationPolicyBuilder().RequireLane());

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	void Require_lane_refuses_blank_or_empty_lanes(string? lane) =>
		Should.Throw<ArgumentException>(() =>
			new AuthorizationPolicyBuilder().RequireLane(NorseLanes.Identity, lane!));

	[Fact]
	void Require_lane_copies_the_lanes_it_is_given()
	{
		string[] lanes = [NorseLanes.Machine];
		AuthorizationPolicyBuilder policy = new();

		policy.RequireLane(lanes);
		lanes[0] = NorseLanes.Identity;

		policy.Requirements.OfType<LaneRequirement>().Single().Lanes.ShouldBe([NorseLanes.Machine]);
	}

	[Fact]
	void Require_audience_applies_to_every_lane_by_default() =>
		new AuthorizationPolicyBuilder().RequireAudience(NorseAudiences.User).Requirements
			.ShouldHaveSingleItem()
			.ShouldBe(new AudienceRequirement(NorseAudiences.User, null));

	[Fact]
	void Require_audience_carries_the_lane_it_is_scoped_to() =>
		new AuthorizationPolicyBuilder().RequireAudience(NorseAudiences.User, NorseLanes.User).Requirements
			.ShouldHaveSingleItem()
			.ShouldBe(new AudienceRequirement(NorseAudiences.User, NorseLanes.User));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	void Require_audience_refuses_a_blank_audience(string? audience) =>
		Should.Throw<ArgumentException>(() => new AuthorizationPolicyBuilder().RequireAudience(audience!));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	void Require_audience_refuses_a_blank_lane(string lane) =>
		Should.Throw<ArgumentException>(() =>
			new AuthorizationPolicyBuilder().RequireAudience(NorseAudiences.User, lane));
}
