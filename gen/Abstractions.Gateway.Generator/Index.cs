using System.ComponentModel;

#pragma warning disable IDE0130
namespace System;
#pragma warning restore IDE0130

/// <summary>
/// Represents a type that can be used to index a collection either from the start or the end.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
readonly struct Index
{
	public int Value { get; }
	public bool IsFromEnd { get; }

	public Index(int value, bool fromEnd = false)
	{
		Value = value;
		IsFromEnd = fromEnd;
	}

	public int GetOffset(int length) => IsFromEnd ? length - Value : Value;

	public static Index Start => new(0);
	public static Index End => new(0, true);
	public static Index FromStart(int value) => new(value);
	public static Index FromEnd(int value) => new(value, true);
}

/// <summary>
/// Represents a range that has start and end indexes.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
readonly struct Range
{
	public Index Start { get; }
	public Index End { get; }

	public Range(Index start, Index end)
	{
		Start = start;
		End = end;
	}

	public (int offset, int length) GetOffsetAndLength(int length)
	{
		var startOffset = Start.GetOffset(length);
		var endOffset = End.GetOffset(length);
		return (startOffset, endOffset - startOffset);
	}

	public static Range StartAt(Index start) => new(start, Index.End);
	public static Range EndAt(Index end) => new(Index.Start, end);
	public static Range All => new(Index.Start, Index.End);
}
