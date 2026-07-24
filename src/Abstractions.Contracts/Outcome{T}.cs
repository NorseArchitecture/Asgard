#pragma warning disable IDE0005 // Using directive is unnecessary
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
	/// <exception cref="ArgumentNullException"><paramref name="value"/> carries a null <see cref="Problem"/> — a smuggled <c>default(Failed)</c> past the case type's own guard.</exception>
	public Outcome(Failed value)
	{
		ArgumentNullException.ThrowIfNull(value.Problem);
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
	/// <exception cref="ArgumentNullException"><paramref name="success"/> or <paramref name="failure"/> is null.</exception>
	/// <exception cref="SwitchExpressionException">This value was defaulted rather than constructed.</exception>
	public TResult Match<TResult>(Func<T, TResult> success, Func<Problem, TResult> failure)
	{
		ArgumentNullException.ThrowIfNull(success);
		ArgumentNullException.ThrowIfNull(failure);
		return this switch
		{
			Success<T>(var value) => success(value),
			Failed(var problem) => failure(problem),
		};
	}

	/// <summary>
	/// Lifts a success payload into the union. Standard DU ergonomics (the equivalent of Rust's
	/// <c>From</c>/<c>into</c>) — a handler may <c>return response;</c> directly instead of
	/// <c>return Outcome&lt;T&gt;.Ok(response);</c>. Union API on its own merits, not wire
	/// knowledge — this type still carries no serialization awareness.
	/// </summary>
	[SuppressMessage("Design", "CA2225:Operator overloads have named alternates", Justification = "Ok(T) is the named alternate — this operator exists purely as ergonomic sugar over it, a second method with a different name would be pure duplication.")]
	public static implicit operator Outcome<T>(T value) => Ok(value);

	/// <summary>
	/// Unwraps the success payload. Throws for a failed outcome — a caller that cannot prove
	/// success ahead of time must pattern-match instead of converting.
	/// </summary>
	/// <exception cref="InvalidOperationException">This outcome is the <see cref="Failed"/> case.</exception>
	[SuppressMessage("Design", "CA2225:Operator overloads have named alternates", Justification = "Match(...) is the named alternate — this operator exists purely as ergonomic sugar over it, a second method with a different name would be pure duplication.")]
	public static explicit operator T(Outcome<T> outcome) => outcome.Match(
		static ok => ok,
		static problem => throw new InvalidOperationException(
			$"Cannot convert a failed Outcome<{typeof(T).Name}> to its success payload (category: {problem.Category})."));
}
