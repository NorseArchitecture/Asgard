namespace Norse.Abstractions.Components.Primitives;

/// <summary>
///     Declares the host's app-shell layout, so a component that needs to nest inside it (via
///     <see cref="Microsoft.AspNetCore.Components.LayoutView" />) never takes a build-time reference to
///     the host's concrete layout type. No rendering, no persistence — pure declared law, per Asgard's
///     charter.
/// </summary>
public interface IAppShellLayout
{
	/// <summary>
	///     Gets the layout component type the host renders as its outermost chrome.
	/// </summary>
	Type LayoutType { get; }
}
