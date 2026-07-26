using System.Text;

namespace Norse.Abstractions.Emit.Tests;

public sealed class CSharpEmitTests
{
	[Fact]
	void AppendCSharp_IsIdenticalToAppendLine()
	{
		const string Code = "public static class Foo\n{\n}";

		var viaAppendCSharp = new StringBuilder().AppendCSharp(Code).ToString();
		var viaAppendLine = new StringBuilder().AppendLine(Code).ToString();

		viaAppendCSharp.ShouldBe(viaAppendLine);
	}
}
