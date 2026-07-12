using Norse.Abstractions.Components.Primitives;

namespace Norse.Abstractions.Components.Tests.Primitives;

public sealed class IDashboardWidgetTests
{
	[Fact]
	void Title_returns_concrete_value()
	{
		StubWidget widget = new();

		widget.Title.ShouldBe("Stub Widget");
	}

	[Fact]
	void ComponentType_returns_concrete_value()
	{
		StubWidget widget = new();

		widget.ComponentType.ShouldBe(typeof(StubWidgetComponent));
	}

	sealed class StubWidget : IDashboardWidget
	{
		public string Title => "Stub Widget";
		public Type ComponentType => typeof(StubWidgetComponent);
	}

	sealed class StubWidgetComponent;
}
