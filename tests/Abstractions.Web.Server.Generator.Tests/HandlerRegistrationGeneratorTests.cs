using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
		generated.ShouldContain("AddScoped<global::FluentValidation.IValidator<global::Norse.Identity.Web.Server.LoginRequest>, global::Norse.Identity.Web.Server.LoginRequestValidator>");
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
			"AddScoped<global::FluentValidation.IValidator<global::Norse.Identity.Web.Server.LoginCommand>, " +
			"global::Norse.Abstractions.Web.Server.Mediator.CommandRequestValidator<global::Norse.Identity.Web.Server.LoginCommand, " +
			"global::Norse.Identity.Web.Server.LoginWire, global::Norse.Abstractions.Contracts.BoolResponse>>");
	}

	[Fact]
	void Also_registers_the_wire_types_own_validator_under_IValidator_of_the_wire_type()
	{
		// The adapter's ctor resolves IEnumerable<IValidator<TWire>> from DI — without this
		// registration the wire type's real validator (Heimdall's, in production) would never run.
		var generated = Generate(WrapperContract);
		generated.ShouldContain(
			"AddScoped<global::FluentValidation.IValidator<global::Norse.Identity.Web.Server.LoginWire>, " +
			"global::Norse.Identity.Web.Server.LoginWireValidator>");
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
