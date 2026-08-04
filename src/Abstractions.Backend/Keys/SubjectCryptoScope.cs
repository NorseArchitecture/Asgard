namespace Norse.Abstractions.Backend.Keys;

/// <summary>
/// The ambient write-subject for payload encryption. Exists because
/// <c>IPersonalDataProtector.Protect(string)</c> carries no subject parameter: writers (the user
/// store, the shred ceremony) establish the subject around the operation; readers never need it —
/// ciphertext is self-describing. A protector asked to encrypt with no ambient subject fails loudly.
/// </summary>
public static class SubjectCryptoScope
{
	static readonly AsyncLocal<Guid?> _ambient = new();

	/// <summary>The ambient subject, or <see langword="null"/> outside any scope.</summary>
	public static Guid? CurrentSubject =>
		_ambient.Value;

	/// <summary>Establishes the ambient subject; disposing restores the prior value (nesting allowed).</summary>
	public static IDisposable Begin(Guid subjectId)
	{
		var prior = _ambient.Value;
		_ambient.Value = subjectId;
		return new Scope(prior);
	}

	sealed class Scope(Guid? prior) : IDisposable
	{
		public void Dispose() =>
			_ambient.Value = prior;
	}
}
