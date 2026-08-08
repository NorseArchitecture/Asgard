namespace Norse.Abstractions.Contracts;

/// <summary>
///     The self-auditing proof an <see cref="ErrorCategory.Erased" /> answer carries when the producer is
///     crypto-shredding: the Syn ledger reference ("severed on X, receipt Y"). A content tombstone —
///     the other legitimate producer of <see cref="ErrorCategory.Erased" /> — carries no receipt.
/// </summary>
/// <param name="ReceiptId">The permanent Syn ledger entry identifier.</param>
/// <param name="SeveredAt">When the subject was severed.</param>
public sealed record ErasureReceipt(Guid ReceiptId, DateTimeOffset SeveredAt);
