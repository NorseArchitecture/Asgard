#pragma warning disable IDE0005 // Using directive is unnecessary
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Norse.Primitives;
#pragma warning restore IDE0005

namespace Norse.Abstractions.Contracts;

/// <summary>
/// The mediator's application-level result vehicle: exactly one of <see cref="Success{T}"/> (reused
/// directly from Svartalfheim) or <see cref="Failed"/>, as a native C# union. Match against the case
/// types — never against <c>Outcome&lt;T&gt;</c> itself; the compiler rejects <c>outcome is Outcome&lt;T&gt;</c>
/// (CS8121). Do not use <c>default(Outcome&lt;T&gt;)</c>; a defaulted value is malformed by construction
/// and throws <see cref="SwitchExpressionException"/> on first exhaustive-switch consumption.
/// Void-success operations use <c>T = </c><see cref="Unit"/> — spelled bare as <c>Outcome</c> via the
/// platform-wide alias, never a second, non-generic type.
/// </summary>
/// <typeparam name="T">The success payload's type. Non-nullable by construction.</typeparam>
[MustConsume]
[Union]
public readonly record struct Outcome<T> : IUnion where T : notnull
{
	enum State : byte { Default = 0, Success = 1, Failure = 2 }

	readonly Success<T> _success;
	readonly Failed _failed;
	readonly State _state;

	/// <summary>Creates a successful outcome. Also reachable as an implicit union conversion.</summary>
	public Outcome(Success<T> value)
	{
		_success = value;
		_state = State.Success;
	}

	/// <summary>Creates a failed outcome. Also reachable as an implicit union conversion.</summary>
	public Outcome(Failed value)
	{
		_failed = value;
		_state = State.Failure;
	}

	/// <summary>
	/// The boxed case contents, or <see langword="null"/> for a defaulted value.
	/// Pattern matching does not read this property; a direct read boxes.
	/// </summary>
	public object? Value =>
		_state switch
		{
			State.Success => _success,
			State.Failure => _failed,
			_ => null,
		};

	/// <summary>Retrieves the success case without boxing.</summary>
	public bool TryGetValue(out Success<T> value)
	{
		value = _success;
		return _state == State.Success;
	}

	/// <summary>Retrieves the failure case without boxing.</summary>
	public bool TryGetValue(out Failed value)
	{
		value = _failed;
		return _state == State.Failure;
	}

	/// <summary>Creates a successful outcome with the given value.</summary>
	[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Outcome<T>.Ok/Err are the type's only construction path by design — an instance-side factory would need an already-constructed Outcome<T> to call it from.")]
	public static Outcome<T> Ok(T value) => new(new Success<T>(value));

	/// <summary>Creates a failed outcome with the given error category and optional field errors.</summary>
	[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Outcome<T>.Ok/Err are the type's only construction path by design — an instance-side factory would need an already-constructed Outcome<T> to call it from.")]
	public static Outcome<T> Err(ErrorCategory category, IReadOnlyDictionary<string, string[]>? errors = null, Guid? correlationId = null) =>
		new(new Failed(new Problem { Category = category, Errors = errors ?? new Dictionary<string, string[]>(), CorrelationId = correlationId }));

	/// <summary>Consumes the outcome by handling both cases.</summary>
	public TResult Match<TResult>(Func<T, TResult> success, Func<Problem, TResult> failure) =>
		this switch
		{
			Success<T>(var value) => success(value),
			Failed(var problem) => failure(problem),
		};
}
