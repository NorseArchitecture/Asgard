using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Norse.Abstractions.Emit;

/// <summary>
/// Emission helper for Roslyn generator projects — see the house style rule in Bifröst's own
/// CLAUDE.md: generator emitter code always calls <see cref="AppendCSharp"/>, never
/// <see cref="StringBuilder.AppendLine(string)"/> directly, collapsing what would otherwise be
/// multiple sequential AppendLine calls into a single raw string literal.
/// </summary>
public static class CSharpEmit
{
	/// <param name="sb">The string builder to append to.</param>
	extension(StringBuilder sb)
	{
		/// <summary>
		/// Appends the given C# code to the string builder, followed by a single line feed.
		/// Always <c>\n</c>, never <see cref="Environment.NewLine"/> — generated source must be
		/// byte-identical regardless of the build machine's OS, matching this platform's
		/// deterministic-build convention. The <c>[StringSyntax("C#")]</c> annotation drives
		/// syntax highlighting in VS / Rider.
		/// </summary>
		/// <param name="code">The C# code to append.</param>
		/// <returns>The same string builder instance.</returns>
		public StringBuilder AppendCSharp([StringSyntax("C#")] string code) =>
			sb.Append($"{code}\n");
	}
}
