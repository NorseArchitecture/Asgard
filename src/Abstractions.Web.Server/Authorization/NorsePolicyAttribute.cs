namespace Norse.Abstractions.Web.Server.Authorization;

/// <summary>
///     Declares that the decorated method configures the named authorization policy. The attribute carries
///     the <b>name</b> and the method carries the <b>shape</b> — one declaration with two facets, never two
///     representations that could disagree.
/// </summary>
/// <remarks>
///     Applied to a <c>public static void</c> method taking a single
///     <see cref="Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder" />. Midgard's generator
///     reads this attribute from <b>metadata</b>, so a realm's policies are discoverable when it arrives as
///     a published package — which is how every realm reaches the composition root. Public because the
///     generated registration lives in a different assembly and has to call it.
/// </remarks>
/// <param name="name">The policy name, owned by the declaring realm's <c>{Context}Policies</c> class.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class NorsePolicyAttribute(string name) : Attribute
{
	/// <summary>The declared policy name.</summary>
	public string Name { get; } = name;
}
