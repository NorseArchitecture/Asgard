namespace Norse.Abstractions.Components.Primitives;

/// <summary>
///     Declares a component that can register as a dashboard widget an end user arranges.
///     No rendering, no persistence — pure declared law, per Asgard's charter.
/// </summary>
public interface IDashboardWidget
{
	/// <summary>
	///     Gets the display title shown to the end user arranging the dashboard.
	/// </summary>
	string Title { get; }

	/// <summary>
	///     Gets the component type to render for this widget.
	/// </summary>
	Type ComponentType { get; }
}
