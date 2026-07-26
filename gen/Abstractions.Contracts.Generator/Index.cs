using System.ComponentModel;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System;
#pragma warning restore IDE0130

/// <summary>
/// Represents a type that can be used to index a collection either from the start or the end.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
readonly struct Index(int value, bool fromEnd = false)
{
	public int Value { get; } = value;
	public bool IsFromEnd { get; } = fromEnd;

	public int GetOffset(int length) =>
		IsFromEnd ? length - Value : Value;

	public static Index Start =>
		new(0);

	public static Index End =>
		new(0, true);

	public static Index FromStart(int value) =>
		new(value);

	public static Index FromEnd(int value) =>
		new(value, true);
}

/// <summary>
/// Represents a range that has start and end indexes.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
readonly struct Range(Index start, Index end)
{
	public Index Start { get; } = start;
	public Index End { get; } = end;

	public (int offset, int length) GetOffsetAndLength(int length)
	{
		var startOffset = Start.GetOffset(length);
		var endOffset = End.GetOffset(length);
		return (startOffset, endOffset - startOffset);
	}

	public static Range StartAt(Index start) =>
		new(start, Index.End);

	public static Range EndAt(Index end) =>
		new(Index.Start, end);

	public static Range All =>
		new(Index.Start, Index.End);
}
