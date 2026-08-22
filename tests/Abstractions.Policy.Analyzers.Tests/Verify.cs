using System.Collections.Immutable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Norse.Abstractions.Components.Authorization;

namespace Norse.Abstractions.Policy.Analyzers.Tests;

/// <summary>
///     Analyzer test harness for this project's <see cref="Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer" />s
///     — as distinct from the sibling <c>Abstractions.Web.Server.Generator.Tests</c> project's
///     generator-driver harness, which exercises an <c>IIncrementalGenerator</c>. Compiles the given source
///     against <see cref="ReferenceAssemblies.Net110" /> plus this assembly's own Authorization types, runs
///     <see cref="NorsePolicyDeclarationAnalyzer" />, and asserts on the resulting diagnostics.
/// </summary>
static class Verify
{
	public static AnalyzerVerification Analyzer(string source) => new(source);
}

sealed class AnalyzerVerification(string source)
{
	static readonly MetadataReference[] _references =
	[
		.. ReferenceAssemblies.Net110,
		MetadataReference.CreateFromFile(typeof(AuthorizationPolicyBuilder).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(NorsePolicyAttribute).Assembly.Location)
	];

	async Task<ImmutableArray<Diagnostic>> RunAsync()
	{
		var compilation = CSharpCompilation.Create(
			"Norse.Sample",
			[CSharpSyntaxTree.ParseText(source)],
			_references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var compileErrors = compilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToImmutableArray();
		compileErrors.ShouldBeEmpty($"Fixture failed to compile:\n{string.Join("\n", compileErrors)}");

		var withAnalyzers = compilation.WithAnalyzers([new NorsePolicyDeclarationAnalyzer()]);
		return await withAnalyzers.GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
	}

	public async Task ShouldReportNothing() =>
		(await RunAsync()).ShouldBeEmpty();

	public async Task ShouldReport(string diagnosticId) =>
		(await RunAsync()).ShouldContain(d => d.Id == diagnosticId);
}
