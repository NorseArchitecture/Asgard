using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Backend.Keys;

/// <summary>
///     The payload-plane key seam: custody, wrap/unwrap, and scheduled destruction of per-subject DEKs.
///     Algorithm choices are the platform's (AES-256-GCM), never the provider's — the seam is custody.
/// </summary>
public interface ISubjectKeyStore
{
	/// <summary>The three-state honest read: available, destroyed-with-receipt, or missing.</summary>
	/// <remarks>
	///     A transient failure to reach the underlying vault/store must be surfaced as a thrown
	///     exception — never folded into <see cref="SubjectKeyResult.Missing" /> or
	///     <see cref="SubjectKeyResult.Destroyed" />. Mapping a timeout to <c>Destroyed</c> would tell a
	///     live customer their data was erased and hand them a fabricated receipt; mapping it to
	///     <c>Missing</c> at least produces a survivable, page-worthy <see cref="ErrorCategory.Fault" />.
	/// </remarks>
	ValueTask<SubjectKeyResult> GetAsync(Guid subjectId, CancellationToken cancellationToken = default);

	/// <summary>
	///     Returns the subject's DEK, minting one for a new subject. A destroyed subject never re-keys —
	///     re-registration is a new subject id, so this throws rather than resurrect.
	/// </summary>
	/// <remarks>
	///     Implementations return a caller-owned copy — the caller may zero the returned buffer after use
	///     (e.g. via <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory" />) without
	///     affecting the store's internal state.
	/// </remarks>
	/// <exception cref="KeyDestroyedException">The subject's key was deliberately destroyed.</exception>
	ValueTask<byte[]> GetOrCreateAsync(Guid subjectId, CancellationToken cancellationToken = default);

	/// <summary>
	///     Destroys the subject's key and returns the receipt. Idempotent: a second destruction against a
	///     subject that has a key, or previously had one destroyed, returns the original receipt — the
	///     ledger records one severance.
	/// </summary>
	/// <exception cref="KeyMissingException">
	///     The subject never had a key in this store. Unknown-subject destruction is an incident, not a
	///     fabricated no-op success — it never masquerades as erasure, per <see cref="KeyMissingException" />'s
	///     own doctrine.
	/// </exception>
	ValueTask<ErasureReceipt> DestroyAsync(Guid subjectId, CancellationToken cancellationToken = default);
}
