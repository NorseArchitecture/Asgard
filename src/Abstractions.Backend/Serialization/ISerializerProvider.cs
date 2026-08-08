namespace Norse.Abstractions.Backend.Serialization;

/// <summary>
///     Hands out the registered default-format <see cref="ISerializer" /> for a naming convention.
///     A future format joins by its own DI registration and a composition-root choice — never by
///     widening this contract. <see cref="NamingStrategy.Unspecified" /> is the smuggled sentinel:
///     implementations throw on it.
/// </summary>
public interface ISerializerProvider
{
	/// <summary>Gets the serializer configured for <paramref name="key" />.</summary>
	ISerializer this[NamingStrategy key] { get; }
}
