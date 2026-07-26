using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Norse.Abstractions.Generator;

/// <summary>
/// Emission helper for Roslyn generator projects — see the house style rule in Bifröst's own
/// CLAUDE.md: generator emitter code always calls <see cref="AppendCSharp"/>, never
/// <see cref="StringBuilder.AppendLine(string)"/> directly, collapsing what would otherwise be
/// multiple sequential AppendLine calls into a single raw string literal.
/// </summary>
public static class CSharpEmit
{
	/// <summary>
	/// Appends the given C# code to the string builder. Identical to
	/// <see cref="StringBuilder.AppendLine(string)"/> at runtime; the
	/// <c>[StringSyntax("C#")]</c> annotation drives syntax highlighting in VS / Rider.
	/// </summary>
	/// <param name="sb">The string builder to append to.</param>
	/// <param name="code">The C# code to append.</param>
	/// <returns>The same string builder instance.</returns>
	public static StringBuilder AppendCSharp(this StringBuilder sb, [StringSyntax("C#")] string code) =>
		sb.AppendLine(code);
}
