using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Norse.Abstractions.Components.Authorization;

namespace Norse.Abstractions.Components.Tests.Authorization;

public sealed class NorsePolicyDeclarationTests
{
	static MethodInfo Declaration(string name) =>
		typeof(NorsePlatformPolicies)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(m => m.GetCustomAttribute<NorsePolicyAttribute>()?.Name == name);

	static AuthorizationPolicy Build(string name)
	{
		AuthorizationPolicyBuilder builder = new();
		Declaration(name).Invoke(null, [builder]);
		return builder.Build();
	}

	[Fact]
	void The_platform_standard_names_are_namespaced_to_Norse()
	{
		NorsePolicies.Anonymous.ShouldBe("Norse.Anonymous");
		NorsePolicies.Probe.ShouldBe("Norse.Probe");
		NorsePolicies.Machine.ShouldBe("Norse.Machine");
		NorsePolicies.RecentAuthentication.ShouldBe("Norse.RecentAuthentication");
	}

	[Fact]
	void The_anonymous_role_name_is_the_one_the_browser_lane_mints() =>
		NorseRoles.Anonymous.ShouldBe("anonymous");

	[Fact]
	void All_four_platform_policies_are_declared_in_metadata()
	{
		var declared = typeof(NorsePlatformPolicies)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Select(m => m.GetCustomAttribute<NorsePolicyAttribute>()?.Name)
			.Where(name => name is not null)
			.ToArray();

		declared.ShouldBe(
			[NorsePolicies.Anonymous, NorsePolicies.Probe, NorsePolicies.Machine, NorsePolicies.RecentAuthentication],
			ignoreOrder: true);
	}

	[Fact]
	void Every_declaration_has_the_signature_the_generator_will_emit_a_call_to()
	{
		foreach (var method in typeof(NorsePlatformPolicies)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(m => m.GetCustomAttribute<NorsePolicyAttribute>() is not null))
		{
			method.ReturnType.ShouldBe(typeof(void));
			method.GetParameters().Select(p => p.ParameterType)
				.ShouldBe([typeof(AuthorizationPolicyBuilder)]);
		}
	}

	[Fact]
	void The_anonymous_policy_requires_a_principal() =>
		Build(NorsePolicies.Anonymous).Requirements
			.ShouldContain(r => r is DenyAnonymousAuthorizationRequirement);

	[Fact]
	void Probe_allows_any_lane()
	{
		// AuthorizationPolicy's constructor throws InvalidOperationException on an empty requirement set
		// (verified against aspnetcore AuthorizationPolicy.cs), so "requires nothing" cannot be expressed as
		// zero requirements. The lane opt-out plus the always-succeed assertion is exactly right here: a
		// kubelet carries no principal at all, so the policy names no lane and demands no principal.
		var requirements = Build(NorsePolicies.Probe).Requirements;

		requirements.Count.ShouldBe(2);
		requirements.ShouldContain(r => r is AnyLaneRequirement);
		requirements.ShouldContain(r => r is AssertionRequirement);
		requirements.ShouldNotContain(r => r is LaneRequirement);
	}

	[Fact]
	void The_probe_policy_does_not_demand_a_principal() =>
		Build(NorsePolicies.Probe).Requirements
			.ShouldNotContain(r => r is DenyAnonymousAuthorizationRequirement);

	[Fact]
	void Machine_requires_machine_lane_and_facade_audience()
	{
		var requirements = Build(NorsePolicies.Machine).Requirements;

		requirements.Count.ShouldBe(2);
		requirements.OfType<LaneRequirement>().Single().Lanes.ShouldBe([NorseLanes.Machine]);
		requirements.OfType<AudienceRequirement>().Single().ShouldBe(new(NorseAudiences.Facade, null));
	}

	[Fact]
	void Recent_authentication_allows_any_lane_and_requires_freshness()
	{
		var requirements = Build(NorsePolicies.RecentAuthentication).Requirements;

		requirements.Count.ShouldBe(2);
		requirements.ShouldContain(r => r is AnyLaneRequirement);
		requirements.ShouldContain(r => r is RecentAuthenticationRequirement);
	}

	[Fact]
	void The_anonymous_policy_declares_no_lane_of_its_own() =>
		// Midgard's generated AddNorsePolicies() closes it to the Identity lane; the declaration is unchanged.
		Build(NorsePolicies.Anonymous).Requirements
			.ShouldNotContain(r => r is LaneRequirement || r is AnyLaneRequirement);

	[Fact]
	void The_machine_policy_does_not_pin_a_scheme()
	{
		// Asgard cannot reference NorseSchemes.Machine (Midgard) -- the dependency wall runs one direction.
		// Scheme selection is entirely NorseLaneSelector's job (Midgard); this policy checks the stamped
		// lane and the audience instead, and like Anonymous and Probe it pins no scheme.
		Build(NorsePolicies.Machine).AuthenticationSchemes.ShouldBeEmpty();
	}
}
