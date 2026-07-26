using System.Text;

namespace Norse.Abstractions.Emit.Tests;

public sealed class CSharpEmitTests
{
	[Fact]
	void AppendCSharp_AlwaysTerminatesWithLineFeed_RegardlessOfPlatformNewline()
	{
		const string Code = "public static class Foo\n{\n}";

		var result = new StringBuilder().AppendCSharp(Code).ToString();

		// Always "\n", never Environment.NewLine — generated source must be byte-identical
		// regardless of the build machine's OS.
		result.ShouldBe($"{Code}\n");
	}
}
