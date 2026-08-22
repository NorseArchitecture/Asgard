using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Norse.Abstractions.Web.Server.Generator;

/// <summary>
///     NORSE015 in the project that authors the declaration. Ships bundled in
///     <c>Norse.Abstractions.Web.Server</c> for the same reason NORSE010/011 do: it keys on this assembly's
///     own <c>NorsePolicyAttribute</c>, and every realm declaring a policy already references this package
///     to name the attribute at all.
/// </summary>
/// <remarks>
///     Shares its validation rules with Midgard's policy generator and its diagnostic id. The split is by
///     provenance, not by rule: this analyzer sees source, the generator sees metadata, and neither sees
///     what the other does.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NorsePolicyDeclarationAnalyzer : DiagnosticAnalyzer
{
	const string AttributeMetadataName = "Norse.Abstractions.Web.Server.Authorization.NorsePolicyAttribute";
	const string BuilderMetadataName = "Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder";

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		[Diagnostics.InvalidPolicyDeclaration];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(start =>
		{
			var attribute = start.Compilation.GetTypeByMetadataName(AttributeMetadataName);
			var builder = start.Compilation.GetTypeByMetadataName(BuilderMetadataName);
			if (attribute is null)
				return;

			start.RegisterSymbolAction(symbol => Inspect(symbol, attribute, builder), SymbolKind.Method);
		});
	}

	static void Inspect(SymbolAnalysisContext context, INamedTypeSymbol attribute, INamedTypeSymbol? builder)
	{
		var method = (IMethodSymbol)context.Symbol;
		var declaration = method.GetAttributes().FirstOrDefault(a =>
			SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute));
		if (declaration is null)
			return;

		var reason = Reason(method, declaration, builder);
		if (reason is null)
			return;

		// Non-null by construction here: a symbol action on this compilation's own source always has one.
		var location = declaration.ApplicationSyntaxReference is { } reference ?
			Location.Create(reference.SyntaxTree, reference.Span) :
			method.Locations.FirstOrDefault() ?? Location.None;

		context.ReportDiagnostic(Diagnostic.Create(
			Diagnostics.InvalidPolicyDeclaration, location,
			$"{method.ContainingType.ToDisplayString()}.{method.Name}", reason));
	}

	static string? Reason(IMethodSymbol method, AttributeData declaration, INamedTypeSymbol? builder)
	{
		// Deliberately not list-pattern syntax (`is [{ ... }]`): netstandard2.0's reference assemblies
		// don't define System.Index, which Roslyn's list-pattern binder requires even for a fixed-length,
		// non-slice pattern -- CS0518/CS0656 in this generator's own compilation. Length + indexer checks
		// below are semantically identical, just spelled without the feature this TFM can't support.
		if (declaration.ConstructorArguments.Length != 1 ||
			declaration.ConstructorArguments[0].Value is not string name ||
			string.IsNullOrWhiteSpace(name))
			return "the policy name must be a non-empty constant string";
		if (!method.IsStatic)
			return "the method must be static";
		if (method.DeclaredAccessibility != Accessibility.Public)
			return "the method must be public -- generated registration lives in another assembly";
		if (method.IsGenericMethod || method.ContainingType.IsGenericType)
			return "neither the method nor its containing type may be generic";
		if (!method.ReturnsVoid)
			return "the method must return void";
		if (method.IsAsync)
			return "the method must not be declared async -- generated registration invokes it synchronously";

		return method.Parameters.Length == 1
			&& builder is not null
			&& SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, builder) ?
			null :
			"the method must take exactly one AuthorizationPolicyBuilder parameter";
	}
}
