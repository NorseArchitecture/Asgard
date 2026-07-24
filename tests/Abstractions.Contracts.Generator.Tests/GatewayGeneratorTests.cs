#pragma warning disable IDE0005
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Norse.Abstractions.Gateway.Generator;
using Shouldly;
#pragma warning restore IDE0005

namespace Norse.Abstractions.Gateway.Generator.Tests;

static class GeneratorTestHarness
{
	public static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) Run(string source, string emissionMode)
	{
		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[CSharpSyntaxTree.ParseText(source)],
			ReferenceAssemblies.Net110.Concat(
			[
				MetadataReference.CreateFromFile(typeof(System.ServiceModel.ServiceContractAttribute).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Norse.Abstractions.Contracts.GenerateGatewayAttribute).Assembly.Location),
			]),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var options = new TestAnalyzerConfigOptionsProvider(emissionMode);
		var driver = CSharpGeneratorDriver.Create([new GatewayGenerator().AsSourceGenerator()])
			.WithUpdatedAnalyzerConfigOptions(options)
			.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

		var generatedSources = outputCompilation.SyntaxTrees.Skip(1).Select(tree => tree.ToString()).ToArray();
		return (diagnostics, generatedSources);
	}
}

public sealed class GatewayGeneratorTests
{
	const string ServiceInterfaceSource = """
		using System.ServiceModel;
		using Microsoft.AspNetCore.Authorization;
		using Norse.Abstractions.Contracts;

		namespace TestRealm.Services;

		[GenerateGateway]
		[ServiceContract]
		public interface IWidgetService
		{
			[Authorize(Policy = "Widget.Read")]
			[OperationContract]
			Task<WidgetResponse> GetWidget(WidgetRequest request, CancellationToken cancellationToken = default);
		}

		public sealed record WidgetRequest;
		public sealed record WidgetResponse;
		""";

	[Fact]
	void ContractMode_EmitsGatewayInterface_MirroringMethodsWrappedInOutcome()
	{
		var (diagnostics, sources) = GeneratorTestHarness.Run(ServiceInterfaceSource, "Contract");

		diagnostics.ShouldBeEmpty();
		var gatewaySource = sources.ShouldHaveSingleItem();
		gatewaySource.ShouldContain("public interface IWidgetGateway");
		gatewaySource.ShouldContain("ValueTask<Outcome<WidgetResponse>> GetWidget(WidgetRequest request, CancellationToken cancellationToken = default)");
	}

	[Fact]
	void ContractMode_VoidSuccessMethod_EmitsOutcomeOfUnitExplicitly_NeverBareAlias()
	{
		const string Source = """
			using System.ServiceModel;
			using Microsoft.AspNetCore.Authorization;
			using Norse.Abstractions.Contracts;

			namespace TestRealm.Services;

			[GenerateGateway]
			[ServiceContract]
			public interface IWidgetService
			{
				[Authorize(Policy = "Widget.Delete")]
				[OperationContract]
				Task DeleteWidget(WidgetRequest request, CancellationToken cancellationToken = default);
			}

			public sealed record WidgetRequest;
			""";

		var (diagnostics, sources) = GeneratorTestHarness.Run(Source, "Contract");

		diagnostics.ShouldBeEmpty();
		var gatewaySource = sources.ShouldHaveSingleItem();
		// Explicit Outcome<Unit> — never bare "ValueTask<Outcome>", which would only compile in a
		// project that happens to carry the GlobalUsings.Outcome.cs alias. Generated code must never
		// depend on that (2026-07-24 review) — this is exactly the gap that broke Task 10's build.
		gatewaySource.ShouldContain("ValueTask<Outcome<Unit>> DeleteWidget(WidgetRequest request, CancellationToken cancellationToken = default)");
		gatewaySource.ShouldNotContain("ValueTask<Outcome> ");
	}

	[Fact]
	void MissingAuthorizeAttribute_ReportsNorse001Error()
	{
		const string Source = """
			using System.ServiceModel;
			using Norse.Abstractions.Contracts;

			namespace TestRealm.Services;

			[GenerateGateway]
			[ServiceContract]
			public interface IWidgetService
			{
				[OperationContract]
				Task<WidgetResponse> GetWidget(WidgetRequest request, CancellationToken cancellationToken = default);
			}

			public sealed record WidgetRequest;
			public sealed record WidgetResponse;
			""";

		var (diagnostics, _) = GeneratorTestHarness.Run(Source, "Contract");

		diagnostics.ShouldContain(d => d.Id == "NORSE001" && d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	void StreamingMethod_ReportsNorse002Error()
	{
		const string Source = """
			using System.ServiceModel;
			using Microsoft.AspNetCore.Authorization;
			using Norse.Abstractions.Contracts;

			namespace TestRealm.Services;

			[GenerateGateway]
			[ServiceContract]
			public interface IWidgetService
			{
				[Authorize(Policy = "Widget.Read")]
				[OperationContract]
				IAsyncEnumerable<WidgetResponse> StreamWidgets(WidgetRequest request, CancellationToken cancellationToken = default);
			}

			public sealed record WidgetRequest;
			public sealed record WidgetResponse;
			""";

		var (diagnostics, sources) = GeneratorTestHarness.Run(Source, "Contract");

		diagnostics.ShouldContain(d => d.Id == "NORSE002" && d.Severity == DiagnosticSeverity.Error);
		sources.ShouldBeEmpty();
	}

	[Fact]
	void InterfaceNotEndingInService_ReportsNorse003Error_DoesNotGuessAName()
	{
		const string Source = """
			using System.ServiceModel;
			using Microsoft.AspNetCore.Authorization;
			using Norse.Abstractions.Contracts;

			namespace TestRealm.Services;

			[GenerateGateway]
			[ServiceContract]
			public interface IWidgetApi
			{
				[Authorize(Policy = "Widget.Read")]
				[OperationContract]
				Task<WidgetResponse> GetWidget(WidgetRequest request, CancellationToken cancellationToken = default);
			}

			public sealed record WidgetRequest;
			public sealed record WidgetResponse;
			""";

		var (diagnostics, sources) = GeneratorTestHarness.Run(Source, "Contract");

		diagnostics.ShouldContain(d => d.Id == "NORSE003" && d.Severity == DiagnosticSeverity.Error);
		sources.ShouldBeEmpty();
	}

	[Fact]
	void MethodWithNoParameters_ReportsNorse004Error_DoesNotCrash()
	{
		const string Source = """
			using System.ServiceModel;
			using Microsoft.AspNetCore.Authorization;
			using Norse.Abstractions.Contracts;

			namespace TestRealm.Services;

			[GenerateGateway]
			[ServiceContract]
			public interface IWidgetService
			{
				[Authorize(Policy = "Widget.Read")]
				[OperationContract]
				Task<WidgetResponse> GetWidget();
			}

			public sealed record WidgetResponse;
			""";

		var (diagnostics, sources) = GeneratorTestHarness.Run(Source, "Contract");

		diagnostics.ShouldContain(d => d.Id == "NORSE004" && d.Severity == DiagnosticSeverity.Error);
		sources.ShouldBeEmpty();
	}

	[Fact]
	void WireHostMode_EmitsWireGateway_DecodingRpcExceptionViaMidgardExtension()
	{
		var (diagnostics, sources) = GeneratorTestHarness.Run(ServiceInterfaceSource, "WireHost");

		diagnostics.ShouldBeEmpty();
		var wireSource = sources.ShouldHaveSingleItem();
		wireSource.ShouldContain("sealed class WidgetWireGateway");
		wireSource.ShouldContain("IWidgetGateway");
		wireSource.ShouldContain("catch (global::Grpc.Core.RpcException ex)");
		wireSource.ShouldContain("global::Norse.Infrastructure.Web.Client.Grpc.RpcExceptionExtensions.DecodeProblem(ex)");
	}

	[Fact]
	void InProcessHostMode_EmitsChainInCorrectOrder_TelemetryOutermost()
	{
		var (diagnostics, sources) = GeneratorTestHarness.Run(ServiceInterfaceSource, "InProcessHost");

		diagnostics.ShouldBeEmpty();
		var source = sources.ShouldHaveSingleItem();
		source.ShouldContain("sealed class WidgetInProcessGateway : IWidgetGateway");
		source.ShouldContain("IValidator<WidgetRequest>");
		source.ShouldContain("AuthenticationStateProvider");
		source.ShouldNotContain("IHttpContextAccessor");
		source.ShouldContain("\"Widget.Read\"");

		var telemetryIndex = source.IndexOf("TelemetryBehavior", StringComparison.Ordinal);
		var exceptionIndex = source.IndexOf("ExceptionTranslationBehavior", StringComparison.Ordinal);
		var authorizationIndex = source.IndexOf("AuthorizationBehavior", StringComparison.Ordinal);
		var validationIndex = source.IndexOf("ValidationBehavior", StringComparison.Ordinal);

		telemetryIndex.ShouldBeLessThan(exceptionIndex);
		exceptionIndex.ShouldBeLessThan(authorizationIndex);
		authorizationIndex.ShouldBeLessThan(validationIndex);
	}

	[Fact]
	void InProcessHostMode_VoidSuccessMethod_UsesUnitViaSameChainShape_NotASecondFamily()
	{
		const string Source = """
			using System.ServiceModel;
			using Microsoft.AspNetCore.Authorization;
			using Norse.Abstractions.Contracts;

			namespace TestRealm.Services;

			[GenerateGateway]
			[ServiceContract]
			public interface IWidgetService
			{
				[Authorize(Policy = "Widget.Delete")]
				[OperationContract]
				Task DeleteWidget(WidgetRequest request, CancellationToken cancellationToken = default);
			}

			public sealed record WidgetRequest;
			""";

		var (diagnostics, sources) = GeneratorTestHarness.Run(Source, "InProcessHost");

		diagnostics.ShouldBeEmpty();
		var generated = sources.ShouldHaveSingleItem();
		generated.ShouldContain("ValueTask<Outcome<Unit>> DeleteWidget(");
		generated.ShouldContain("ValidationBehavior<WidgetRequest, Unit>");
		generated.ShouldContain("AwaitThenUnit");
		// No second behavior/chain family — same generic types the payload-bearing method uses.
		generated.ShouldNotContain("IBehavior<WidgetRequest>");
	}
}
