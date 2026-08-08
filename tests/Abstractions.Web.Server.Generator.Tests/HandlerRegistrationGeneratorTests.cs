using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Web.Server.Mediator;

namespace Norse.Abstractions.Web.Server.Generator.Tests;

public sealed class HandlerRegistrationGeneratorTests
{
	const string Contract = """
		using Microsoft.AspNetCore.Authorization;
		using Norse.Abstractions.Contracts;
		using Norse.Abstractions.Web.Server.Mediator;
		using FluentValidation;
		using System.Threading;
		using System.Threading.Tasks;

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

	// Generate / GenerateDiagnostics: build CSharpCompilation (assembly name "Norse.Identity.Web.Server",
	// references: recovered ReferenceAssemblies + Norse.Abstractions.Contracts + Norse.Abstractions.Web.Server
	// + FluentValidation + Microsoft.AspNetCore.Authorization), run HandlerRegistrationGenerator via
	// CSharpGeneratorDriver, return the single generated tree's text / the driver diagnostics.

	static readonly MetadataReference[] _extraReferences =
	[
		MetadataReference.CreateFromFile(
			typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(FluentValidation.IValidator<>).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Abstractions.Contracts.BoolResponse).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Server.Mediator.ISenderDispatch).Assembly.Location)
	];

	[Fact]
	void Emits_handler_dispatch_and_validator_registrations_named_for_the_assembly()
	{
		var generated = Generate(Contract);
		generated.ShouldContain("AddNorseIdentityWebServerHandlers");
		generated.ShouldContain(
			"\t\tglobal::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(\n\t\t\tservices, global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Scoped(\n\t\t\t\ttypeof(global::Norse.Abstractions.Web.Server.Mediator.IRequestHandler<global::Norse.Identity.Web.Server.LoginRequest, global::Norse.Abstractions.Contracts.BoolResponse>), typeof(global::Norse.Identity.Web.Server.LoginHandler)));");
		generated.ShouldContain(
			"\t\tglobal::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(\n\t\t\tservices, global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton(\n\t\t\t\ttypeof(global::Norse.Abstractions.Web.Server.Mediator.ISenderDispatch), typeof(global::Norse.Abstractions.Web.Server.Mediator.SenderDispatch<global::Norse.Identity.Web.Server.LoginRequest, global::Norse.Abstractions.Contracts.BoolResponse>)));");
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
	void Registering_twice_resolves_each_validator_handler_and_dispatch_entry_exactly_once()
	{
		// No pre-existing compile-and-invoke harness lived in this project (the rest of this file
		// only ever inspects generated source as text) — CompileAndLoad below emits the generator's
		// output to a real in-memory assembly exactly once, so LoginRequest/LoginRequestValidator are
		// the same runtime Type across both invocations and TryAddEnumerable has something to dedupe
		// against; two separate compilations would produce two distinct Types that could never collide.
		//
		// This test used to only resolve IValidator<LoginRequest> — proving the validator half of the
		// idempotent-registration shape, but never exercising the handler/dispatch half, which is
		// exactly the gap that let AddScoped/AddSingleton ship unconverted alongside the validator fix.
		// It now also builds the dispatch map the way Midgard's SenderDispatchMap does
		// (entries.ToFrozenDictionary(entry => entry.RequestType)) and asserts that doesn't throw —
		// ToFrozenDictionary throws ArgumentException on a duplicate key, which is exactly what a
		// second plain AddSingleton<ISenderDispatch, SenderDispatch<...>> would have produced.
		var assembly = CompileAndLoad(Contract);
		var registration =
			assembly.GetType("Norse.Identity.Web.Server.NorseHandlerRegistration")!.GetMethod(
				"AddNorseIdentityWebServerHandlers")!;

		void InvokeGeneratedRegistration(IServiceCollection services) =>
			registration.Invoke(null, [services]);

		ServiceCollection services = new();
		InvokeGeneratedRegistration(services);
		InvokeGeneratedRegistration(services);

		using var provider = services.BuildServiceProvider();
		var loginRequestType = assembly.GetType("Norse.Identity.Web.Server.LoginRequest")!;
		var loginRequestValidatorType = typeof(FluentValidation.IValidator<>).MakeGenericType(loginRequestType);
		provider.GetServices(loginRequestValidatorType).ShouldHaveSingleItem();

		var handlerType =
			typeof(IRequestHandler<,>).MakeGenericType(loginRequestType, typeof(Abstractions.Contracts.BoolResponse));
		provider.GetServices(handlerType).ShouldHaveSingleItem();

		var dispatchEntries = provider.GetServices<ISenderDispatch>();
		dispatchEntries.ShouldHaveSingleItem();
		Should.NotThrow(() => dispatchEntries.ToFrozenDictionary(entry => entry.RequestType));
	}

	[Fact]
	void Two_handlers_wrapping_the_same_wire_type_do_not_double_register_the_wire_validator()
	{
		// WireValidatorTypeNames is computed per-handler (WrapperContract-style Distinct() scoped to
		// one h.Request at a time, not across handlers) — so two handlers in the SAME assembly, both
		// wrapping the same wire DTO, each independently carry the wire type's validator in their own
		// model and the emitter writes two TryAddEnumerable calls for the identical descriptor from a
		// SINGLE generator run. This proves that in-run duplicate collapses to one provider entry too,
		// not just the across-two-runs case the test above covers.
		const string TwoHandlersSameWireType = """
			using Microsoft.AspNetCore.Authorization;
			using Norse.Abstractions.Contracts;
			using Norse.Abstractions.Web.Server.Mediator;
			using FluentValidation;
			using System.Threading;
			using System.Threading.Tasks;

			namespace Norse.Identity.Web.Server;

			public sealed record LoginWire;

			[Authorize(Policy = "AuthN.Public")]
			public sealed record LoginCommand(LoginWire Request) : CommandRequest<LoginWire, BoolResponse>(Request);

			[Authorize(Policy = "AuthN.Public")]
			public sealed record LoginAgainCommand(LoginWire Request) : CommandRequest<LoginWire, BoolResponse>(Request);

			sealed class LoginCommandHandler : IRequestHandler<LoginCommand, BoolResponse>
			{
				public ValueTask<Outcome<BoolResponse>> Handle(LoginCommand request, CancellationToken cancellationToken = default) =>
					ValueTask.FromResult(Outcome<BoolResponse>.Ok(new BoolResponse { Value = true }));
			}

			sealed class LoginAgainCommandHandler : IRequestHandler<LoginAgainCommand, BoolResponse>
			{
				public ValueTask<Outcome<BoolResponse>> Handle(LoginAgainCommand request, CancellationToken cancellationToken = default) =>
					ValueTask.FromResult(Outcome<BoolResponse>.Ok(new BoolResponse { Value = true }));
			}

			public sealed class LoginWireValidator : AbstractValidator<LoginWire>;
			""";

		var assembly = CompileAndLoad(TwoHandlersSameWireType);
		var registration =
			assembly.GetType("Norse.Identity.Web.Server.NorseHandlerRegistration")!.GetMethod(
				"AddNorseIdentityWebServerHandlers")!;

		ServiceCollection services = new();
		registration.Invoke(null, [services]);

		using var provider = services.BuildServiceProvider();
		var loginWireType = assembly.GetType("Norse.Identity.Web.Server.LoginWire")!;
		var loginWireValidatorType = typeof(FluentValidation.IValidator<>).MakeGenericType(loginWireType);
		provider.GetServices(loginWireValidatorType).ShouldHaveSingleItem();

		var dispatchEntries = provider.GetServices<ISenderDispatch>();
		dispatchEntries.Count()
			.ShouldBe(2); // one per distinct request type (LoginCommand, LoginAgainCommand), never collapsed
		Should.NotThrow(() => dispatchEntries.ToFrozenDictionary(entry => entry.RequestType));
	}

	// CompileAndLoad: the emission-only tests above never call Emit (they only ToString() the
	// generated syntax tree, no semantic binding required), so ReferenceAssemblies.Net110 +
	// _extraReferences was always enough for them. This method is the only caller that performs a
	// real Emit, and needs two more assemblies (ServiceCollection/IServiceCollection) on top of that
	// same set — computed locally, not as a shared static field, so a reference-resolution failure
	// here can only ever fail Registering_twice_resolves_each_validator_exactly_once, not the whole
	// class's static initialization.
	static Assembly CompileAndLoad(string source)
	{
		MetadataReference[] references =
		[
			.. ReferenceAssemblies.Net110,
			.. _extraReferences,
			MetadataReference.CreateFromFile(typeof(ServiceCollection).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location)
		];

		var compilation = CSharpCompilation.Create(
			"Norse.Identity.Web.Server",
			[CSharpSyntaxTree.ParseText(source)],
			references,
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
