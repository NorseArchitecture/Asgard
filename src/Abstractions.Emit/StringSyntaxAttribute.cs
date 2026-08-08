// Polyfill for System.Diagnostics.CodeAnalysis.StringSyntaxAttribute (added in .NET 7). Every
// Roslyn generator project targets netstandard2.0 regardless of what its consumer targets, so the
// BCL definition isn't available there. Roslyn's IDE classifiers recognize the attribute by
// namespace + type name (not assembly identity), so a declaration here still drives the
// embedded-language hint in VS / Rider for any consumer of this package.

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics.CodeAnalysis;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
///     Polyfill for <c>System.Diagnostics.CodeAnalysis.StringSyntaxAttribute</c>. Marks a parameter
///     or field as containing embedded language code for IDE syntax highlighting.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class StringSyntaxAttribute(string syntax) : Attribute
{
	/// <summary>
	///     Gets the name of the embedded language (e.g., "C#", "SQL", "HTML").
	/// </summary>
	public string Syntax { get; } = syntax;
}
