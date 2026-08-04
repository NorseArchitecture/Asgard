namespace Norse.Abstractions.Backend.Keys;

/// <summary>
/// The lookup-plane keyring: service-level, rotatable, producing the keys blind indexes are HMAC'd
/// under. Deliberately not per-subject — you must find the user before you know whose key to use.
/// Rotation is a re-hash ceremony over all current rows, never a config flip.
/// </summary>
public interface ILookupKeyRing
{
	/// <summary>The key id new blind indexes are written under.</summary>
	string CurrentKeyId { get; }

	/// <summary>Every key id the ring can still answer for (rotation window).</summary>
	IEnumerable<string> KeyIds { get; }

	/// <summary>Resolves a key by id.</summary>
	/// <exception cref="KeyNotFoundException">The id is not on the ring.</exception>
	byte[] GetKey(string keyId);
}
