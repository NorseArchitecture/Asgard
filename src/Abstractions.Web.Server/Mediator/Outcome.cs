using System.Diagnostics.CodeAnalysis;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// The mediator's application-level result vehicle for operations with no success payload. Server-only,
/// in-process — never serialized directly. The channel boundary (gRPC forwarder, JSON controller) maps
/// success to the transport's own success signal and decomposes failure into that transport's native
/// error mechanism (RpcException/trailers, ProblemDetails).
/// </summary>
public sealed record Outcome
{
	/// <summary>
	/// Indicates whether the operation succeeded.
	/// </summary>
	public required bool IsSuccess { get; init; }

	/// <summary>
	/// The error detail, present only when the operation failed.
	/// </summary>
	public Problem? Problem { get; init; }

	/// <summary>
	/// Creates a successful outcome.
	/// </summary>
	public static Outcome Ok() => new() { IsSuccess = true };

	/// <summary>
	/// Creates a failed outcome with the given error category and optional field errors.
	/// </summary>
	public static Outcome Err(ErrorCategory category, IReadOnlyDictionary<string, string[]>? errors = null) =>
		new() { IsSuccess = false, Problem = new Problem { Category = category, Errors = errors ?? new Dictionary<string, string[]>() } };
}

/// <summary>
/// The mediator's application-level result vehicle for operations with a success payload of type
/// <typeparamref name="T"/>. Server-only, in-process — never serialized directly; see <see cref="Outcome"/>.
/// </summary>
public sealed record Outcome<T>
{
	/// <summary>
	/// Indicates whether the operation succeeded.
	/// </summary>
	public required bool IsSuccess { get; init; }

	/// <summary>
	/// The success payload, present only when the operation succeeded.
	/// </summary>
	public T? Value { get; init; }

	/// <summary>
	/// The error detail, present only when the operation failed.
	/// </summary>
	public Problem? Problem { get; init; }

	/// <summary>
	/// Creates a successful outcome with the given value.
	/// </summary>
	[SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
		Justification = "Outcome<T>.Ok/Err are the type's only construction path by design — an instance-side factory would need an already-constructed Outcome<T> to call it from.")]
	public static Outcome<T> Ok(T value) => new() { IsSuccess = true, Value = value };

	/// <summary>
	/// Creates a failed outcome with the given error category and optional field errors.
	/// </summary>
	[SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
		Justification = "Outcome<T>.Ok/Err are the type's only construction path by design — an instance-side factory would need an already-constructed Outcome<T> to call it from.")]
	public static Outcome<T> Err(ErrorCategory category, IReadOnlyDictionary<string, string[]>? errors = null) =>
		new() { IsSuccess = false, Problem = new Problem { Category = category, Errors = errors ?? new Dictionary<string, string[]>() } };
}
