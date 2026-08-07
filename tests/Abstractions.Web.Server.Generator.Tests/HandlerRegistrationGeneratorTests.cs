using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace Norse.Abstractions.Web.Server.Generator.Tests;

public sealed class HandlerRegistrationGeneratorTests
{
	const string Contract = """
		using Microsoft.AspNetCore.Authorization;
		using Norse.Abstractions.Contracts;
		using Norse.Abstractions.Web.Server.Mediator;
		using FluentValidation;

		namespace Norse.Identity.Web.Server;

		[Authorize(Policy = "AuthN.Public")]
		public sealed record LoginRequest : ICommandRequest<BoolResponse>;

		sealed class LoginHandler : IRequestHandler<LoginRequest, BoolResponse>
		{
			public ValueTask<Outcome<BoolResponse>> Handle(LoginRequest request, CancellationToken cancellationToken = default) =>
				ValueTask.FromResult(Outcome<BoolResponse>.Ok(new BoolResponse { Value = true }));
		}

		public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>;
		""";

	[Fact]
	void Emits_handler_dispatch_and_validator_registrations_named_for_the_assembly()
	{
		var generated = Generate(Contract);
		generated.ShouldContain("AddNorseIdentityWebServerHandlers");
		generated.ShouldContain("AddScoped<global::Norse.Abstractions.Web.Server.Mediator.IRequestHandler<global::Norse.Identity.Web.Server.LoginRequest, global::Norse.Abstractions.Contracts.BoolResponse>, global::Norse.Identity.Web.Server.LoginHandler>");
		generated.ShouldContain("AddSingleton<global::Norse.Abstractions.Web.Server.Mediator.ISenderDispatch, global::Norse.Abstractions.Web.Server.Mediator.SenderDispatch<global::Norse.Identity.Web.Server.LoginRequest, global::Norse.Abstractions.Contracts.BoolResponse>>");
		generated.ShouldContain(
			"\t\tglobal::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(\n\t\t\tservices, global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Scoped(\n\t\t\t\ttypeof(global::FluentValidation.IValidator<global::Norse.Identity.Web.Server.LoginRequest>), typeof(global::Norse.Identity.Web.Server.LoginRequestValidator)));");
	}

	[Fact]
	void Validator_registrations_are_emitted_in_ordinal_order_regardless_of_declaration_order()
	{
		// ZetaValidator is declared before AlphaValidator — reverse of ordinal order — so a pass
		// here can only mean the emitter sorts, not that it happens to preserve source/symbol
		// enumeration order (spec: "same input -> identical bytes" requires this be deterministic
		// regardless of Roslyn's undocumented symbol-enumeration order).
		const string TwoValidatorsReverseOrdinal = """
			using Microsoft.AspNetCore.Authorization;
			using Norse.Abstractions.Contracts;
			using Norse.Abstractions.Web.Server.Mediator;
			using FluentValidation;

			namespace Norse.Identity.Web.Server;

			[Authorize(Policy = "AuthN.Public")]
			public sealed record LoginRequest : ICommandRequest<BoolResponse>;

			sealed class LoginHandler : IRequestHandler<LoginRequest, BoolResponse>
			{
				public ValueTask<Outcome<BoolResponse>> Handle(LoginRequest request, CancellationToken cancellationToken = default) =>
					ValueTask.FromResult(Outcome<BoolResponse>.Ok(new BoolResponse { Value = true }));
			}

			public sealed class ZetaValidator : AbstractValidator<LoginRequest>;
			public sealed class AlphaValidator : AbstractValidator<LoginRequest>;
			""";

		var generated = Generate(TwoValidatorsReverseOrdinal);

		var alphaIndex = generated.IndexOf("Norse.Identity.Web.Server.AlphaValidator", StringComparison.Ordinal);
		var zetaIndex = generated.IndexOf("Norse.Identity.Web.Server.ZetaValidator", StringComparison.Ordinal);
		alphaIndex.ShouldBeGreaterThan(-1);
		zetaIndex.ShouldBeGreaterThan(-1);
		alphaIndex.ShouldBeLessThan(zetaIndex);
	}

	[Fact]
	void NORSE011_fires_when_a_handled_request_carries_no_authorize_policy()
	{
		var withoutAuthorize = Contract.Replace("[Authorize(Policy = \"AuthN.Public\")]", "");
		var diagnostics = GenerateDiagnostics(withoutAuthorize);
		diagnostics.ShouldContain(d => d.Id == "NORSE011" && d.Severity == DiagnosticSeverity.Error);
	}

	const string WrapperContract = """
		using Microsoft.AspNetCore.Authorization;
		using Norse.Abstractions.Contracts;
		using Norse.Abstractions.Web.Server.Mediator;
		using FluentValidation;

		namespace Norse.Identity.Web.Server;

		public sealed record LoginWire;

		[Authorize(Policy = "AuthN.Public")]
		public sealed record LoginCommand(LoginWire Request) : CommandRequest<LoginWire, BoolResponse>(Request);

		sealed class LoginCommandHandler : IRequestHandler<LoginCommand, BoolResponse>
		{
			public ValueTask<Outcome<BoolResponse>> Handle(LoginCommand request, CancellationToken cancellationToken = default) =>
				ValueTask.FromResult(Outcome<BoolResponse>.Ok(new BoolResponse { Value = true }));
		}

		public sealed class LoginWireValidator : AbstractValidator<LoginWire>;
		""";

	[Fact]
	void Emits_the_CommandRequestValidator_adapter_registration_for_a_wrapper_command()
	{
		var generated = Generate(WrapperContract);
		generated.ShouldContain(
			"\t\tglobal::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(\n\t\t\tservices, global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Scoped(\n\t\t\t\ttypeof(global::FluentValidation.IValidator<global::Norse.Identity.Web.Server.LoginCommand>), typeof(global::Norse.Abstractions.Web.Server.Mediator.CommandRequestValidator<global::Norse.Identity.Web.Server.LoginCommand, global::Norse.Identity.Web.Server.LoginWire, global::Norse.Abstractions.Contracts.BoolResponse>)));");
	}

	[Fact]
	void Also_registers_the_wire_types_own_validator_under_IValidator_of_the_wire_type()
	{
		// The adapter's ctor resolves IEnumerable<IValidator<TWire>> from DI — without this
		// registration the wire type's real validator (Heimdall's, in production) would never run.
		var generated = Generate(WrapperContract);
		generated.ShouldContain(
			"\t\tglobal::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(\n\t\t\tservices, global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Scoped(\n\t\t\t\ttypeof(global::FluentValidation.IValidator<global::Norse.Identity.Web.Server.LoginWire>), typeof(global::Norse.Identity.Web.Server.LoginWireValidator)));");
	}

	[Fact]
	void Does_not_emit_the_CommandRequestValidator_adapter_for_a_non_wrapper_request()
	{
		var generated = Generate(Contract);
		generated.ShouldNotContain("CommandRequestValidator");
	}

	[Fact]
	void NORSE010_fires_when_two_handlers_claim_the_same_request()
	{
		// A second namespace-declaration form (block-scoped) alongside Contract's file-scoped
		// `namespace Norse.Identity.Web.Server;` would be CS8955 (a file may not mix the two
		// forms) — SecondLoginHandler lands directly in the tail of the same file-scoped
		// namespace instead, no block wrapper needed.
		var duplicated = $$"""
			{{Contract}}

			sealed class SecondLoginHandler : Norse.Abstractions.Web.Server.Mediator.IRequestHandler<LoginRequest, Norse.Abstractions.Contracts.BoolResponse>
			{
				public ValueTask<Norse.Abstractions.Contracts.Outcome<Norse.Abstractions.Contracts.BoolResponse>> Handle(LoginRequest request, CancellationToken cancellationToken = default) =>
					ValueTask.FromResult(Norse.Abstractions.Contracts.Outcome<Norse.Abstractions.Contracts.BoolResponse>.Ok(new Norse.Abstractions.Contracts.BoolResponse { Value = true }));
			}
			""";
		var diagnostics = GenerateDiagnostics(duplicated);
		diagnostics.ShouldContain(d => d.Id == "NORSE010" && d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	void Registering_twice_resolves_each_validator_exactly_once()
	{
		// No pre-existing compile-and-invoke harness lived in this project (the rest of this file
		// only ever inspects generated source as text) — CompileAndLoad below emits the generator's
		// output to a real in-memory assembly exactly once, so LoginRequest/LoginRequestValidator are
		// the same runtime Type across both invocations and TryAddEnumerable has something to dedupe
		// against; two separate compilations would produce two distinct Types that could never collide.
		var assembly = CompileAndLoad(Contract);
		var registration = assembly.GetType("Norse.Identity.Web.Server.NorseHandlerRegistration")!.GetMethod("AddNorseIdentityWebServerHandlers")!;
		void InvokeGeneratedRegistration(IServiceCollection services) =>
			registration.Invoke(null, [services]);

		ServiceCollection services = new();
		InvokeGeneratedRegistration(services);
		InvokeGeneratedRegistration(services);

		using var provider = services.BuildServiceProvider();
		var loginRequestValidatorType = typeof(FluentValidation.IValidator<>).MakeGenericType(assembly.GetType("Norse.Identity.Web.Server.LoginRequest")!);
		provider.GetServices(loginRequestValidatorType).ShouldHaveSingleItem();
	}

	// Generate / GenerateDiagnostics: build CSharpCompilation (assembly name "Norse.Identity.Web.Server",
	// references: recovered ReferenceAssemblies + Norse.Abstractions.Contracts + Norse.Abstractions.Web.Server
	// + FluentValidation + Microsoft.AspNetCore.Authorization), run HandlerRegistrationGenerator via
	// CSharpGeneratorDriver, return the single generated tree's text / the driver diagnostics.

	static readonly MetadataReference[] _extraReferences =
	[
		MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(FluentValidation.IValidator<>).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Norse.Abstractions.Contracts.BoolResponse).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Norse.Abstractions.Web.Server.Mediator.ISenderDispatch).Assembly.Location),
	];

	// _loadReferences / CompileAndLoad: the emission-only tests above never need a complete reference
	// set (they only ToString() the generated syntax tree — no semantic binding required, so
	// ReferenceAssemblies.Net110's minimal set is enough). This pair exists solely for
	// Registering_twice_resolves_each_validator_exactly_once, which calls Emit and therefore needs
	// every BCL/ASP.NET assembly the Contract fixture and the generated code actually touch
	// (CancellationToken, ValueTask, IServiceProvider, DI) — sourced from the running test host's own
	// trusted platform assemblies rather than hand-curated, so it can't silently fall out of date.
	static readonly MetadataReference[] _loadReferences =
	[
		.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path)),
		MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(FluentValidation.IValidator<>).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Norse.Abstractions.Contracts.BoolResponse).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Norse.Abstractions.Web.Server.Mediator.ISenderDispatch).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(ServiceCollection).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
	];

	// Contract itself carries no "using System.Threading;"/"using System.Threading.Tasks;" — harmless
	// under the emission-only tests above (they never call Emit, so CS0246 on CancellationToken/
	// ValueTask never surfaces), but Emit performs full semantic binding, so this harness supplies
	// the two global usings the MSBuild-driven build would normally inject via ImplicitUsings.
	const string GlobalUsingsPrelude = """
		global using System.Threading;
		global using System.Threading.Tasks;
		""";

	static Assembly CompileAndLoad(string source)
	{
		var compilation = CSharpCompilation.Create(
			"Norse.Identity.Web.Server",
			[CSharpSyntaxTree.ParseText(source), CSharpSyntaxTree.ParseText(GlobalUsingsPrelude)],
			_loadReferences,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		_ = CSharpGeneratorDriver.Create([new HandlerRegistrationGenerator().AsSourceGenerator()])
			.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

		using MemoryStream stream = new();
		var emitResult = outputCompilation.Emit(stream);
		emitResult.Success.ShouldBeTrue(string.Join('\n', emitResult.Diagnostics));
		return Assembly.Load(stream.ToArray());
	}

	static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) Run(string source)
	{
		var compilation = CSharpCompilation.Create(
			"Norse.Identity.Web.Server",
			[CSharpSyntaxTree.ParseText(source)],
			[.. ReferenceAssemblies.Net110, .. _extraReferences],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		_ = CSharpGeneratorDriver.Create([new HandlerRegistrationGenerator().AsSourceGenerator()])
			.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

		string[] generatedSources = [.. outputCompilation.SyntaxTrees.Skip(1).Select(tree => tree.ToString())];
		return (diagnostics, generatedSources);
	}

	static string Generate(string source) =>
		Run(source).GeneratedSources.Single();

	static ImmutableArray<Diagnostic> GenerateDiagnostics(string source) =>
		Run(source).Diagnostics;
}
