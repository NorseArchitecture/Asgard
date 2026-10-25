using Norse.Abstractions.Contracts;
using Norse.Abstractions.Components.ServerValidation;

namespace Norse.Abstractions.Components.Tests.ServerValidation;

public sealed class CategoryDisplayTests
{
	[Fact]
	void Indeterminate_renders_its_own_sentence_rather_than_the_fallback() =>
		CategoryDisplay.For(new() { Category = ErrorCategory.Indeterminate })
			.ShouldBe("This change may or may not have been saved. Reload to check before trying again.");
}
