using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Backend.Keys;

/// <summary>
///     Thrown when decryption meets a deliberately destroyed key — the materialization channel for the
///     seam's <c>Destroyed</c> state (an EF value converter has no return path for a union). Machinery-
///     internal: the disclosure repository's fold catches it and answers <c>ErrorCategory.Erased</c>
///     with the receipt; one that escapes lands in the unhandled-exception interceptor as an honest Fault.
/// </summary>
public sealed class KeyDestroyedException(ErasureReceipt receipt) :
	Exception($"Subject key deliberately destroyed at {receipt.SeveredAt:O}; receipt {receipt.ReceiptId}.")
{
	/// <summary>The Syn ledger proof.</summary>
	public ErasureReceipt Receipt { get; } = receipt;
}
