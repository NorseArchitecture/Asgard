using System.ComponentModel;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130

/// <summary>
///     Reserved to be used by the compiler for tracking metadata about the 'init' keyword and its use.
/// </summary>
/// <remarks>
///     Public — unlike this polyfill's usual internal-by-default shape (e.g. Urðarbrunnr's own copy,
///     which is compiled directly into each consumer via a linked source file, never crossing a real
///     assembly boundary). Here it must be visible from a consuming project's own compilation across
///     a genuine NuGet/ProjectReference boundary, since that project's own <c>init</c>-accessor code
///     (e.g. a positional record) needs to resolve this type from a referenced assembly, not a
///     same-assembly source file.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IsExternalInit;
