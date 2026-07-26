using System.Text;

namespace Norse.Abstractions.Emit.Tests;

public sealed class Utf8NoBomTests
{
	[Fact]
	void Encoding_EmitsNoPreamble() =>
		Utf8NoBom.Encoding.GetPreamble().ShouldBeEmpty();

	[Fact]
	void Encoding_IsStillUtf8() =>
		Utf8NoBom.Encoding.CodePage.ShouldBe(Encoding.UTF8.CodePage);
}
