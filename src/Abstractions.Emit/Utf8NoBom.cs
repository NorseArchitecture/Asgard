using System.Text;

namespace Norse.Abstractions.Emit;

/// <summary>
///     The BOM-free UTF-8 encoding every Norse generator writes with, matching the platform-wide
///     convention that no source file — hand-written or generated — carries a byte-order mark. The
///     <see cref="Encoding.UTF8" /> singleton emits one; this doesn't. Pass it to
///     <c>Microsoft.CodeAnalysis.Text.SourceText.From(code, Utf8NoBom.Encoding)</c> at every
///     <c>AddSource</c> call site instead of relying on the default encoding.
/// </summary>
public static class Utf8NoBom
{
	/// <summary>UTF-8 without a byte-order mark.</summary>
	public static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
}
