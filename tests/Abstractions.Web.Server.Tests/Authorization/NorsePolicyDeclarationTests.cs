using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Norse.Abstractions.Web.Server.Authorization;

namespace Norse.Abstractions.Web.Server.Tests.Authorization;

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
	}

	[Fact]
	void Both_platform_policies_are_declared_in_metadata()
	{
		var declared = typeof(NorsePlatformPolicies)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Select(m => m.GetCustomAttribute<NorsePolicyAttribute>()?.Name)
			.Where(name => name is not null)
			.ToArray();

		declared.ShouldBe([NorsePolicies.Anonymous, NorsePolicies.Probe], ignoreOrder: true);
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
	void The_probe_policy_builds_despite_requiring_nothing()
	{
		// AuthorizationPolicy's constructor throws InvalidOperationException on an empty requirement set
		// (verified against aspnetcore AuthorizationPolicy.cs), so "requires nothing" cannot be expressed as
		// zero requirements. It is one always-succeed assertion, which is a different thing from
		// RequireAuthenticatedUser and is exactly right here: a kubelet carries no principal at all.
		Build(NorsePolicies.Probe).Requirements.Count.ShouldBe(1);
	}

	[Fact]
	void The_probe_policy_does_not_demand_a_principal() =>
		Build(NorsePolicies.Probe).Requirements
			.ShouldNotContain(r => r is DenyAnonymousAuthorizationRequirement);
}
