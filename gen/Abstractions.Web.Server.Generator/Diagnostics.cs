using Microsoft.CodeAnalysis;

namespace Norse.Abstractions.Web.Server.Generator;

#pragma warning disable RS2008 // No analyzer-release ledger, matching the platform's other generators/analyzers.

/// <summary>
///     NORSE015 — the policy-declaration rule, checked here for source in the compilation being built.
///     NORSE010/NORSE011 (duplicate handler, missing authorization policy) are declared inline in
///     <see cref="HandlerRegistrationGenerator" /> rather than here; NORSE015 gets its own descriptor file
///     because <see cref="NorsePolicyDeclarationAnalyzer" /> is a plain <c>DiagnosticAnalyzer</c>, not part
///     of the incremental generator's diagnostics.
/// </summary>
static class Diagnostics
{
	public static readonly DiagnosticDescriptor InvalidPolicyDeclaration = new(
		"NORSE015",
		"Invalid [NorsePolicy] declaration",
		"'{0}' is decorated with [NorsePolicy] but {1}",
		"Norse.Mediator",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
		"A decorated method is either a valid declaration or a build error -- never silently skipped. This "
		+ "analyzer catches declarations in the project that authors them, where the diagnostic has a real "
		+ "source location; Midgard's policy generator enforces the same rule for declarations arriving from "
		+ "referenced assemblies, which have no syntax to point at. The two halves are disjoint, so a "
		+ "declaration is never reported twice.");
}
