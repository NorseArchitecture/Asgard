using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Backend.Keys;

/// <summary>
/// The payload-plane key seam: custody, wrap/unwrap, and scheduled destruction of per-subject DEKs.
/// Algorithm choices are the platform's (AES-256-GCM), never the provider's — the seam is custody.
/// </summary>
public interface ISubjectKeyStore
{
	/// <summary>The three-state honest read: available, destroyed-with-receipt, or missing.</summary>
	ValueTask<SubjectKeyResult> GetAsync(Guid subjectId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the subject's DEK, minting one for a new subject. A destroyed subject never re-keys —
	/// re-registration is a new subject id, so this throws rather than resurrect.
	/// </summary>
	/// <exception cref="KeyDestroyedException">The subject's key was deliberately destroyed.</exception>
	ValueTask<byte[]> GetOrCreateAsync(Guid subjectId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Destroys the subject's key and returns the receipt. Idempotent: a second destruction returns
	/// the original receipt — the ledger records one severance.
	/// </summary>
	ValueTask<ErasureReceipt> DestroyAsync(Guid subjectId, CancellationToken cancellationToken = default);
}
