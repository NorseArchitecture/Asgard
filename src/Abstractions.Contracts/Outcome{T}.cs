#pragma warning disable IDE0005 // Using directive is unnecessary
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Norse.Primitives;
#pragma warning restore IDE0005

namespace Norse.Abstractions.Contracts;

/// <summary>
/// The result of an operation inside Yggdrasil — an event, not data. It is never written to a
/// wire and never stored; its entire purpose is forcing the caller to look into the void and
/// handle the unhappy path without papering over it (the wire-serialization surrogate erases
/// this type entirely — see <c>OutcomeSurrogatesEmitter</c> — a <see cref="Failed"/> reaching any
/// marshaller is a misconfiguration, not a shape the wire ever legitimately carries).
///
/// Exactly one of <see cref="Success{T}"/> (reused directly from Svartalfheim) or
/// <see cref="Failed"/>, as a native C# union — deliberately a <see langword="sealed"/> class, not
/// a struct or a record. Not a struct: this type never sits in a CLR-generic-constrained position
/// that requires a reference type (gRPC client/server proxy machinery among them), and struct-ness
/// was never load-bearing for a boundary envelope created once per operation adjacent to I/O — a
/// jurisdiction where allocation is the workload (Svartalfheim's <see cref="Result{T}"/>) is
/// untouched by this reasoning and stays a struct. Not a record: this is ephemeral control-flow
/// sugar, never stored, never compared, never <c>with</c>-mutated — structural equality is the
/// wrong contract for it.
///
/// Starved API surface is the enforcement mechanism: <see cref="Ok"/>/<see cref="Err"/> are the
/// documented construction path; consumption via <see cref="Match{TResult}"/> or the
/// <c>TryGetValue</c> escape hatches <c>[MustConsume]</c> already blesses. No <em>typed</em>
/// shortcut exposes either case directly (no <c>.Problem</c>, no <c>.Succeeded</c>) — the moment a
/// happy-path shortcut exists, "forcing you to cope" degrades into "politely suggesting you
/// glance." <see cref="Value"/> is the one exception, and it is not a shortcut: the compiler's
/// native union feature requires it to be public (matching Svartalfheim's <see cref="Result{T}"/>,
/// the platform's only other native union), it returns untyped <see langword="object"/>?, and nothing
/// in this type's own machinery reads it — a direct read boxes. Match against the case types —
/// never against <c>Outcome&lt;T&gt;</c> itself; the compiler rejects <c>outcome is Outcome&lt;T&gt;</c>
/// (CS8121).
///
/// A reference to this type must never be null — null is not a legitimate third state alongside
/// <see cref="Success{T}"/>/<see cref="Failed"/>; every construction path (<see cref="Ok"/>,
/// <see cref="Err"/>, the implicit lift from <typeparamref name="T"/>) always returns a real
/// instance. Void-success operations use <c>T = </c><see cref="Unit"/>.
/// </summary>
/// <typeparam name="T">The success payload's type. Non-nullable by construction.</typeparam>
[MustConsume]
[Union]
public sealed class Outcome<T> : IUnion where T : notnull
{
	readonly Success<T>? _success;
	readonly Failed? _failed;

	/// <summary>
	/// Creates a successful outcome. The compiler's native union feature requires this constructor
	/// to be public (a "union creation member") — <see cref="Ok"/> is still the documented,
	/// sanctioned construction path; this constructor is discoverable but not the intended surface.
	/// </summary>
	public Outcome(Success<T> value) => _success = value;

	/// <summary>
	/// Creates a failed outcome. The compiler's native union feature requires this constructor to
	/// be public (a "union creation member") — <see cref="Err"/> is still the documented,
	/// sanctioned construction path; this constructor is discoverable but not the intended surface.
	/// </summary>
	/// <exception cref="ArgumentNullException"><paramref name="value"/> carries a null <see cref="Problem"/> — a smuggled default <see cref="Failed"/> past the case type's own guard.</exception>
	public Outcome(Failed value)
	{
		ArgumentNullException.ThrowIfNull(value.Problem);
		_failed = value;
	}

	/// <summary>
	/// The boxed case contents. The compiler's native union feature requires this property to be
	/// public with a public getter — pattern matching and <see cref="TryGetValue(out Success{T})"/>/
	/// <see cref="TryGetValue(out Failed)"/> never read it directly; a direct read boxes.
	/// </summary>
	public object? Value => (object?)_success ?? _failed;

	/// <summary>Retrieves the success case without boxing.</summary>
	public bool TryGetValue(out Success<T> value)
	{
		value = _success.GetValueOrDefault();
		return _success.HasValue;
	}

	/// <summary>Retrieves the failure case without boxing.</summary>
	public bool TryGetValue(out Failed value)
	{
		value = _failed.GetValueOrDefault();
		return _failed.HasValue;
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
	///
	/// Null-checked despite <typeparamref name="T"/> being <see langword="notnull"/>: protobuf-net's
	/// surrogate contract (<c>SetSurrogate</c>) requires both conversion operators between a
	/// reference-typed real type and its surrogate to pass <see langword="null"/> through unchanged
	/// — its deserializer round-trips a default/no-existing-value merge target through these
	/// operators before populating it. This branch exists for that wire-only scaffolding path, never
	/// for real application code, which cannot construct a null <typeparamref name="T"/> in the first
	/// place under the <see langword="notnull"/> constraint.
	/// </summary>
	[SuppressMessage("Design", "CA2225:Operator overloads have named alternates", Justification = "Ok(T) is the named alternate — this operator exists purely as ergonomic sugar over it, a second method with a different name would be pure duplication.")]
	public static implicit operator Outcome<T>(T value) => value is null ? null! : Ok(value);

	/// <summary>
	/// Unwraps the success payload. Throws for a failed outcome — a caller that cannot prove
	/// success ahead of time must pattern-match instead of converting. This is the one sanctioned
	/// violence in this type's surface, deliberately explicit-cast-shaped so it reads as violence
	/// at the call site.
	///
	/// Null-checked for the same reason as the implicit lift operator above: protobuf-net's
	/// surrogate deserializer calls this on a default/no-existing-value merge target, which is
	/// <see langword="null"/> now that this type is a class — passing <see langword="null"/> through
	/// rather than throwing keeps that wire-only scaffolding path working without weakening the
	/// real, application-facing "throws on Failed" contract below.
	/// </summary>
	/// <exception cref="InvalidOperationException">This outcome is the <see cref="Failed"/> case.</exception>
	[SuppressMessage("Design", "CA2225:Operator overloads have named alternates", Justification = "Match(...) is the named alternate — this operator exists purely as ergonomic sugar over it, a second method with a different name would be pure duplication.")]
	public static explicit operator T(Outcome<T> outcome) => outcome is null ? default! : outcome.Match(
		static ok => ok,
		static problem => throw new InvalidOperationException(
			$"Cannot convert a failed Outcome<{typeof(T).Name}> to its success payload (category: {problem.Category})."));
}
