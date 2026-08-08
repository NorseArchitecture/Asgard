using System.Runtime.CompilerServices;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Backend.Keys;

/// <summary>
///     The key seam's honesty contract, a seam-local closed three-state union: the repository's honesty
///     depends on the vault's honesty (2026-08-03 PII spec §3.1). <c>Available</c> carries the unwrapped
///     DEK; <c>Destroyed</c> carries the Syn receipt — the erased path; <c>Missing</c> — no key and no
///     receipt — is the incident path, never erasure. Deliberately neither <c>Result&lt;T&gt;</c> nor
///     <c>Outcome&lt;T&gt;</c>: the two unions never grow domain-specific arms.
/// </summary>
/// <remarks><c>default(SubjectKeyResult)</c> is malformed; <see cref="Match" /> throws on it.</remarks>
public readonly record struct SubjectKeyResult
{
	readonly byte[]? _key;
	readonly ErasureReceipt? _receipt;
	readonly State _state;

	SubjectKeyResult(byte[]? key, ErasureReceipt? receipt, State state) =>
		(_key, _receipt, _state) = (key, receipt, state);

	/// <summary>No key and no receipt — the incident path.</summary>
	public static SubjectKeyResult Missing { get; } = new(null, null, State.Missing);

	/// <summary>The key exists and is unwrapped.</summary>
	/// <remarks>
	///     Takes ownership of <paramref name="key" /> — the caller constructing this result must not
	///     mutate the array afterward. Implementations are expected to hand this a fresh, caller-owned
	///     copy of the underlying key material, so a consumer reading the <c>Available</c> arm may zero
	///     the returned buffer after use (e.g. via
	///     <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory" />)
	///     without affecting the store's internal state.
	/// </remarks>
	/// <exception cref="ArgumentException"><paramref name="key" /> is empty.</exception>
	public static SubjectKeyResult Available(byte[] key)
	{
		ArgumentNullException.ThrowIfNull(key);
		return key.Length == 0 ?
			throw new ArgumentException("An available key cannot be empty.", nameof(key)) :
			new(key, null, State.Available);
	}

	/// <summary>The key was deliberately destroyed; the receipt proves it.</summary>
	public static SubjectKeyResult Destroyed(ErasureReceipt receipt)
	{
		ArgumentNullException.ThrowIfNull(receipt);
		return new SubjectKeyResult(null, receipt, State.Destroyed);
	}

	/// <summary>The single consumption door — three arms, exhaustive.</summary>
	/// <exception cref="SwitchExpressionException">The malformed <c>default</c> instance.</exception>
	public TResult Match<TResult>(Func<byte[], TResult> available, Func<ErasureReceipt, TResult> destroyed,
		Func<TResult> missing) =>
		_state switch
		{
			State.Available => available(_key!),
			State.Destroyed => destroyed(_receipt!),
			State.Missing => missing(),
			_ => throw new SwitchExpressionException(_state)
		};

	enum State : byte
	{
		Unspecified = 0,
		Available = 1,
		Destroyed = 2,
		Missing = 3
	}
}
