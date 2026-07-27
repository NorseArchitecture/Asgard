using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Norse.Abstractions.Emit;

namespace Norse.Abstractions.Web.Server.Generator;

#pragma warning disable RS2008 // No analyzer-release ledger, matching the platform's other generators.

/// <summary>
/// Discovers a realm's <c>IRequestHandler&lt;,&gt;</c> and <c>IValidator&lt;&gt;</c>
/// implementations at compile time and emits <c>AddNorse{Realm}Handlers()</c> — handler,
/// dispatch-map, and validator registrations, replacing assembly scanning with compile-time
/// wiring (spec §2.7).
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class HandlerRegistrationGenerator : IIncrementalGenerator
{
	static readonly DiagnosticDescriptor _duplicateHandler = new(
		"NORSE010", "Duplicate request handler",
		"Request type '{0}' has more than one IRequestHandler implementation in this assembly", "Norse.Mediator",
		DiagnosticSeverity.Error, isEnabledByDefault: true);

	static readonly DiagnosticDescriptor _missingAuthorizePolicy = new(
		"NORSE011", "Request missing authorization policy",
		"Request type '{0}' carries no [Authorize(Policy = ...)] — every request names its policy, AuthNPolicies.Public included", "Norse.Mediator",
		DiagnosticSeverity.Error, isEnabledByDefault: true);

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var models = context.CompilationProvider.Select(Discover);
		context.RegisterSourceOutput(models, static (productionContext, result) =>
		{
			foreach (var diagnostic in result.Diagnostics)
				productionContext.ReportDiagnostic(diagnostic);
			if (result.Handlers.Length > 0 && !result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
				productionContext.AddSource("NorseHandlerRegistration.g.cs",
					SourceText.From(RegistrationEmitter.Emit(result.AssemblyName, result.RootNamespace, result.Handlers), Utf8NoBom.Encoding));
		});
	}

	static DiscoveryResult Discover(Compilation compilation, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var handlerInterface = compilation.GetTypeByMetadataName("Norse.Abstractions.Web.Server.Mediator.IRequestHandler`2");
		var validatorInterface = compilation.GetTypeByMetadataName("FluentValidation.IValidator`1");
		var authorizeAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Authorization.AuthorizeAttribute");
		if (handlerInterface is null)
			return DiscoveryResult.Empty(compilation);

		// Handlers: compiling assembly only — registration is a realm-local act and the emitted code
		// legally references internal handler types from inside their own assembly.
		var handlers = AllTypes(compilation.Assembly.GlobalNamespace)
			.Where(t => t is { IsAbstract: false, TypeKind: TypeKind.Class })
			.SelectMany(t => t.AllInterfaces
				.Where(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, handlerInterface))
				.Select(i => (Handler: t, Request: i.TypeArguments[0], Response: i.TypeArguments[1])))
			.ToImmutableArray();

		var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

		foreach (var duplicate in handlers.GroupBy(h => h.Request, SymbolEqualityComparer.Default).Where(g => g.Count() > 1))
			diagnostics.Add(Diagnostic.Create(_duplicateHandler, Location.None, duplicate.Key!.ToDisplayString()));

		foreach (var (_, request, _) in handlers)
			if (authorizeAttribute is not null && !request.GetAttributes().Any(a =>
					SymbolEqualityComparer.Default.Equals(a.AttributeClass, authorizeAttribute) &&
					a.NamedArguments.Any(n => n.Key == "Policy" && n.Value.Value is string { Length: > 0 })))
				diagnostics.Add(Diagnostic.Create(_missingAuthorizePolicy, Location.None, request.ToDisplayString()));

		// Validators: compiled-symbol walk across own + referenced assemblies (PackageReference-mode
		// parity) — Heimdall's validators serve Himinbjorg's handlers.
		IAssemblySymbol[] assemblies = [compilation.Assembly, .. compilation.SourceModule.ReferencedAssemblySymbols];
		ImmutableArray<(INamedTypeSymbol Validator, ITypeSymbol Request)> validators = validatorInterface is null ?
			[] :
			[.. assemblies
				.SelectMany(a => AllTypes(a.GlobalNamespace))
				.Where(t => t is { IsAbstract: false, TypeKind: TypeKind.Class })
				.SelectMany(t => t.AllInterfaces
					.Where(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, validatorInterface))
					.Select(i => (Validator: t, Request: i.TypeArguments[0])))];

		var format = SymbolDisplayFormat.FullyQualifiedFormat; // const is illegal on a reference type — local it is
		var models = handlers
			.Select(h => new HandlerModel(
				h.Handler.ToDisplayString(format),
				h.Request.ToDisplayString(format),
				h.Response.ToDisplayString(format),
				[.. validators
					.Where(v => SymbolEqualityComparer.Default.Equals(v.Request, h.Request))
					.Select(v => v.Validator.ToDisplayString(format))
					.Distinct()
					.OrderBy(v => v, StringComparer.Ordinal)]))
			.OrderBy(m => m.RequestTypeName, StringComparer.Ordinal)
			.ToImmutableArray();

		return new(compilation.AssemblyName ?? "Unknown", RootNamespace(compilation), models, diagnostics.ToImmutable());
	}

	static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol root)
	{
		foreach (var member in root.GetMembers())
			switch (member)
			{
				case INamespaceSymbol ns:
					foreach (var nested in AllTypes(ns))
						yield return nested;
					break;
				case INamedTypeSymbol type:
					yield return type;
					break;
			}
	}

	static string RootNamespace(Compilation compilation) =>
		compilation.AssemblyName ?? "Norse.Generated";

	sealed record DiscoveryResult(string AssemblyName, string RootNamespace, ImmutableArray<HandlerModel> Handlers, ImmutableArray<Diagnostic> Diagnostics)
	{
		public static DiscoveryResult Empty(Compilation compilation) =>
			new(compilation.AssemblyName ?? "Unknown", compilation.AssemblyName ?? "Norse.Generated", [], []);
	}
}
