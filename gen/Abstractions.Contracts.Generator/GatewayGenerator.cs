using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Norse.Abstractions.Contracts.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class GatewayGenerator : IIncrementalGenerator
{
#pragma warning disable RS2008
	static readonly DiagnosticDescriptor _missingAuthorize = new(
		"NORSE001", "Service method missing [Authorize]",
		"Method '{0}' on a [GenerateGateway] interface must carry [Authorize(Policy = ...)]. No Asgard-contracted service method may be unprotected by construction.",
		"Norse.Gateway", DiagnosticSeverity.Error, isEnabledByDefault: true);

	static readonly DiagnosticDescriptor _streamingNotSupported = new(
		"NORSE002", "Streaming service methods are not supported by the gateway generator",
		"Method '{0}' returns IAsyncEnumerable<T>. V1 excludes streaming from gateway generation entirely (spec §2.3); remove [GenerateGateway] from this interface or move streaming methods to a separate, ungated interface.",
		"Norse.Gateway", DiagnosticSeverity.Error, isEnabledByDefault: true);

	static readonly DiagnosticDescriptor _unrecognizedInterfaceSuffix = new(
		"NORSE003", "[GenerateGateway] interface name is not I{Context}Service",
		"Interface '{0}' is decorated [GenerateGateway] but its name doesn't match I{{Context}}Service. The generator derives the gateway's name from that suffix and refuses to guess for any other shape (e.g. I{{Context}}Api). Rename the interface or extend this generator's naming rule deliberately.",
		"Norse.Gateway", DiagnosticSeverity.Error, isEnabledByDefault: true);

	static readonly DiagnosticDescriptor _missingRequestParameter = new(
		"NORSE004", "Service method has no request parameter",
		"Method '{0}' on a [GenerateGateway] interface takes no parameters — every gateway-generated method requires exactly one request parameter (spec §2.2); this is a malformed service interface, not a shape the generator can emit code for",
		"Norse.Gateway", DiagnosticSeverity.Error, isEnabledByDefault: true);

	static readonly DiagnosticDescriptor _returnTypeNotOutcome = new(
		"NORSE005", "Service method does not return Task<Outcome<T>> or ValueTask<Outcome<T>>",
		"Method '{0}' on a [GenerateGateway] interface must return Task<Outcome<T>> or ValueTask<Outcome<T>> — every Asgard-contracted service method returns the envelope directly (spec §9, 2026-07-24 amendment); this is a malformed service interface, not a shape the generator can emit code for",
		"Norse.Gateway", DiagnosticSeverity.Error, isEnabledByDefault: true);
#pragma warning restore RS2008

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var emissionMode = context.AnalyzerConfigOptionsProvider.Select(GetEmissionMode);

#pragma warning disable IDE0200
		var interfaces = context.CompilationProvider.Select((compilation, cancellationToken) => Discover(compilation, cancellationToken));
#pragma warning restore IDE0200

		context.RegisterSourceOutput(interfaces.Combine(emissionMode), (productionContext, pair) =>
		{
			var (discovered, mode) = pair;
			foreach (var (model, diagnostics) in discovered)
			{
				foreach (var diagnostic in diagnostics)
					productionContext.ReportDiagnostic(diagnostic);

				if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
					continue;

				if (mode == "Contract")
					productionContext.AddSource($"{model.ContextName}Gateway.g.cs", ContractEmitter.Emit(model));
				else if (mode == "WireHost")
					productionContext.AddSource($"{model.ContextName}WireGateway.g.cs", WireHostEmitter.Emit(model));
				else if (mode == "InProcessHost")
					productionContext.AddSource($"{model.ContextName}InProcessGateway.g.cs", InProcessHostEmitter.Emit(model));
			}
		});
	}

	static string GetEmissionMode(AnalyzerConfigOptionsProvider provider, CancellationToken _) =>
		provider.GlobalOptions.TryGetValue("build_property.NorseGatewayEmissionMode", out var mode) ? mode : "Contract";

	static ImmutableArray<(GatewayInterfaceModel Model, ImmutableArray<Diagnostic> Diagnostics)> Discover(Compilation compilation, CancellationToken cancellationToken)
	{
		var results = ImmutableArray.CreateBuilder<(GatewayInterfaceModel, ImmutableArray<Diagnostic>)>();
		var generateGatewayAttribute = compilation.GetTypeByMetadataName("Norse.Abstractions.Contracts.GenerateGatewayAttribute");
		var authorizeAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Authorization.AuthorizeAttribute");
		if (generateGatewayAttribute is null)
			return results.ToImmutable();

		// Compiled-symbol walk (own module + every referenced assembly), never source syntax trees — PackageReference-mode parity.
		foreach (var assembly in new[] { compilation.Assembly }.Concat(compilation.SourceModule.ReferencedAssemblySymbols))
		{
			cancellationToken.ThrowIfCancellationRequested();
			foreach (var type in GetAllTypes(assembly.GlobalNamespace))
			{
				if (type.TypeKind != TypeKind.Interface)
					continue;
				if (!type.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, generateGatewayAttribute)))
					continue;

				var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
				var methods = ImmutableArray.CreateBuilder<GatewayMethodModel>();
				foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
				{
					if (member.ReturnType is INamedTypeSymbol { Name: "IAsyncEnumerable" })
					{
						diagnostics.Add(Diagnostic.Create(_streamingNotSupported, member.Locations.FirstOrDefault() ?? Location.None, member.Name));
						continue;
					}
					var authorize = member.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, authorizeAttribute));
					if (authorize is null)
					{
						diagnostics.Add(Diagnostic.Create(_missingAuthorize, member.Locations.FirstOrDefault() ?? Location.None, member.Name));
						continue;
					}
					if (member.Parameters.Length == 0)
					{
						diagnostics.Add(Diagnostic.Create(_missingRequestParameter, member.Locations.FirstOrDefault() ?? Location.None, member.Name));
						continue;
					}
					var policyName = authorize.NamedArguments.FirstOrDefault(kv => kv.Key == "Policy").Value.Value as string ?? "";
					var requestType = member.Parameters[0].Type.Name;
					var responseType = ExtractOutcomePayloadType(member.ReturnType);
					if (responseType is null)
					{
						diagnostics.Add(Diagnostic.Create(_returnTypeNotOutcome, member.Locations.FirstOrDefault() ?? Location.None, member.Name));
						continue;
					}
					methods.Add(new GatewayMethodModel(member.Name, requestType, responseType, policyName));
				}

				// Spec §9.8-style naming: I{Context}Service is the only shape this generator derives a
				// context name from. I{Context}Api (also a real shape per the spec's own scoping note)
				// or any other suffix reports a diagnostic instead of silently slicing an arbitrary
				// number of characters off the end of the name (2026-07-24 review, lesser finding 1).
				if (!type.Name.StartsWith("I", StringComparison.Ordinal) || !type.Name.EndsWith("Service", StringComparison.Ordinal) || type.Name.Length <= 1 + "Service".Length)
				{
					diagnostics.Add(Diagnostic.Create(_unrecognizedInterfaceSuffix, type.Locations.FirstOrDefault() ?? Location.None, type.Name));
					results.Add((new GatewayInterfaceModel(type.ContainingNamespace.ToDisplayString(), type.Name, type.Name, methods.ToImmutable()), diagnostics.ToImmutable()));
					continue;
				}
				var contextName = type.Name.Substring(1, type.Name.Length - 1 - "Service".Length);
				results.Add((new GatewayInterfaceModel(type.ContainingNamespace.ToDisplayString(), type.Name, contextName, methods.ToImmutable()), diagnostics.ToImmutable()));
			}
		}
		return results.ToImmutable();
	}

	// member.ReturnType is expected to be Task<Outcome<T>> or ValueTask<Outcome<T>> — every
	// Asgard-contracted service method returns the envelope directly (spec §9, 2026-07-24
	// amendment); this unwraps two levels (the awaitable, then Outcome<T> itself) to reach T,
	// rather than the pre-amendment single-level unwrap that wrapped a bare payload.
	static string? ExtractOutcomePayloadType(ITypeSymbol returnType)
	{
		if (returnType is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } awaitable)
			return null;
		if (awaitable.TypeArguments[0] is not INamedTypeSymbol { Name: "Outcome", IsGenericType: true, TypeArguments.Length: 1 } outcome)
			return null;
		return outcome.TypeArguments[0].Name;
	}

	static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
	{
		foreach (var member in root.GetMembers())
		{
			if (member is INamespaceSymbol ns)
			{
				foreach (var nested in GetAllTypes(ns))
					yield return nested;
			}
			else if (member is INamedTypeSymbol type)
			{
				yield return type;
			}
		}
	}
}
