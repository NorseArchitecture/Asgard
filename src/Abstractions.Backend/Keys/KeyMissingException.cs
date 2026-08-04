namespace Norse.Abstractions.Backend.Keys;

/// <summary>
/// Thrown when decryption meets a key that should exist and does not — no key, no receipt. An
/// incident: it pages someone; it never masquerades as erasure. Deliberately uncaught by the
/// disclosure fold so the exception-translation behavior renders it a Fault with a correlation id.
/// </summary>
public sealed class KeyMissingException(Guid subjectId) :
	Exception($"Subject key for {subjectId} is missing with no destruction receipt — incident, not erasure.")
{
	/// <summary>The subject whose key is missing.</summary>
	public Guid SubjectId { get; } = subjectId;
}
