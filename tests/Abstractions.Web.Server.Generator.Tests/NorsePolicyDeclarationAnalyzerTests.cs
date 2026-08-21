namespace Norse.Abstractions.Web.Server.Generator.Tests;

public sealed class NorsePolicyDeclarationAnalyzerTests
{
	const string Preamble = """
		using Microsoft.AspNetCore.Authorization;
		using Norse.Abstractions.Web.Server.Authorization;
		""";

	// Non-static wrapper, deliberately: a static class cannot host an instance member at all (CS0708), and
	// one malformed case below (a non-static declaration) needs to compile so the analyzer -- not the
	// C# compiler -- is what strikes it. A non-static class hosts both static and instance members, so
	// every other case (all of which are static) still means what it says.
	static string Declaring(string member) => $$"""
		{{Preamble}}
		public class Sample
		{
			{{member}}
		}
		""";

	[Fact]
	async Task Accepts_a_well_formed_declaration() =>
		await Verify.Analyzer(Declaring("""
			[NorsePolicy("Sample.Public")]
			public static void ConfigurePublic(AuthorizationPolicyBuilder policy) =>
				policy.RequireAuthenticatedUser();
			""")).ShouldReportNothing();

	[Theory]
	[InlineData("""[NorsePolicy("X")] static void M(AuthorizationPolicyBuilder p) { }""")]
	[InlineData("""[NorsePolicy("X")] internal static void M(AuthorizationPolicyBuilder p) { }""")]
	[InlineData("""[NorsePolicy("X")] public void M(AuthorizationPolicyBuilder p) { }""")]
	[InlineData("""[NorsePolicy("X")] public static int M(AuthorizationPolicyBuilder p) => 0;""")]
	[InlineData("""[NorsePolicy("X")] public static void M() { }""")]
	[InlineData("""[NorsePolicy("X")] public static void M(string s) { }""")]
	[InlineData("""[NorsePolicy("X")] public static void M(AuthorizationPolicyBuilder p, int extra) { }""")]
	[InlineData("""[NorsePolicy("X")] public static void M<T>(AuthorizationPolicyBuilder p) { }""")]
	[InlineData("""[NorsePolicy("")] public static void M(AuthorizationPolicyBuilder p) { }""")]
	[InlineData("""[NorsePolicy(null)] public static void M(AuthorizationPolicyBuilder p) { }""")]
	async Task Strikes_a_malformed_declaration(string member) =>
		await Verify.Analyzer(Declaring(member)).ShouldReport("NORSE015");

	[Fact]
	async Task Strikes_a_declaration_on_a_generic_containing_type() =>
		await Verify.Analyzer($$"""
			{{Preamble}}
			public static class Outer<T>
			{
				[NorsePolicy("X")]
				public static void M(AuthorizationPolicyBuilder p) { }
			}
			""").ShouldReport("NORSE015");

	[Fact]
	async Task Ignores_an_undecorated_private_method() =>
		await Verify.Analyzer(Declaring("static void M(AuthorizationPolicyBuilder p) { }"))
			.ShouldReportNothing();
}
