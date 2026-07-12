using Bunit;

namespace Norse.Abstractions.Components.Tests;

public sealed class LoaderTests : BunitContext
{
	[Fact]
	void Renders_default_label()
	{
		var cut = Render<Loader>();

		cut.Find("[role='status']").GetAttribute("aria-label").ShouldBe("Loading…");
	}

	[Fact]
	void Renders_custom_label()
	{
		var cut = Render<Loader>(parameters => parameters
			.Add(p => p.Label, "Fetching widgets…"));

		cut.Find("[role='status']").GetAttribute("aria-label").ShouldBe("Fetching widgets…");
	}
}
