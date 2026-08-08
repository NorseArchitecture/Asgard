using System.Net.Mime;

namespace Norse.Abstractions.Backend.Serialization;

/// <summary>
///     Format-agnostic payload serialization: objects to and from bytes, streams, and strings. The
///     surface is deliberately pure BCL — no serializer machinery type ever crosses it, so realms
///     declare intent here while the encoding executes behind the wire border (NORSE070). The default
///     case is JSON, said by <see cref="ContentType" />'s default — but nothing constrains an
///     implementation to it: any format that can honor this surface registers through DI and drops in.
/// </summary>
public interface ISerializer
{
	/// <summary>The MIME content type this serializer produces and consumes. Defaults to <c>application/json</c>.</summary>
	string ContentType =>
		MediaTypeNames.Application.Json;

	/// <summary>Whether <see cref="DeserializeAsync{T}" /> is genuinely asynchronous. Defaults to <see langword="true" />.</summary>
	bool HasAsyncSupport =>
		true;

	/// <summary>Deserializes a <typeparamref name="T" /> from a raw byte payload.</summary>
	T? Deserialize<T>(byte[] bytes);

	/// <summary>Deserializes a <typeparamref name="T" /> from a stream.</summary>
	T? Deserialize<T>(Stream stream);

	/// <summary>Deserializes a <typeparamref name="T" /> from a string payload.</summary>
	T? Deserialize<T>(string payload);

	/// <summary>Asynchronously deserializes a <typeparamref name="T" /> from a stream.</summary>
	ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

	/// <summary>Serializes <paramref name="obj" /> to <paramref name="stream" />.</summary>
	/// <param name="stream">The destination stream.</param>
	/// <param name="obj">The object to serialize.</param>
	/// <param name="serializeNulls">When <see langword="true" />, null properties are written.</param>
	void Serialize<T>(Stream stream, T obj, bool serializeNulls = false);

	/// <summary>Serializes <paramref name="obj" /> to a string.</summary>
	/// <param name="obj">The object to serialize.</param>
	/// <param name="serializeNulls">When <see langword="true" />, null properties are written.</param>
	/// <param name="prettyPrint">When <see langword="true" />, the output is human-formatted.</param>
	string Serialize<T>(T obj, bool serializeNulls = false, bool prettyPrint = false);

	/// <summary>Asynchronously serializes <paramref name="obj" /> to <paramref name="stream" />.</summary>
	Task SerializeAsync<T>(Stream stream, T obj, bool serializeNulls = false,
		CancellationToken cancellationToken = default);

	/// <summary>Serializes <paramref name="obj" /> directly to UTF-8 bytes.</summary>
	byte[] SerializeToUtf8Bytes<T>(T obj, bool serializeNulls = false);
}
