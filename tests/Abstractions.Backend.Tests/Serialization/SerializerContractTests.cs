using Norse.Abstractions.Backend.Serialization;

namespace Norse.Abstractions.Backend.Tests.Serialization;

public sealed class SerializerContractTests
{
	[Fact]
	void Content_type_defaults_to_json_and_async_defaults_to_supported()
	{
		ISerializer serializer = new BareSerializer();
		serializer.ContentType.ShouldBe("application/json");
		serializer.HasAsyncSupport.ShouldBeTrue();
	}

	[Theory]
	[InlineData(NamingStrategy.Unspecified, 0)]
	[InlineData(NamingStrategy.CamelCase, 1)]
	[InlineData(NamingStrategy.PascalCase, 2)]
	[InlineData(NamingStrategy.SnakeCase, 3)]
	[InlineData(NamingStrategy.KebabCase, 4)]
	void Naming_strategy_values_are_explicit_and_zero_is_the_sentinel(NamingStrategy strategy, int value) =>
		((int)strategy).ShouldBe(value);

	// DIM-default probe: the contract's defaults are law (spec §1) — a format that overrides
	// neither is JSON-shaped and async-capable by declaration.
	sealed class BareSerializer : ISerializer
	{
		public T? Deserialize<T>(byte[] bytes) => default;
		public T? Deserialize<T>(Stream stream) => default;
		public T? Deserialize<T>(string payload) => default;

		public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default) =>
			default;

		public void Serialize<T>(Stream stream, T obj, bool serializeNulls = false) { }
		public string Serialize<T>(T obj, bool serializeNulls = false, bool prettyPrint = false) => "";

		public Task SerializeAsync<T>(Stream stream, T obj, bool serializeNulls = false,
			CancellationToken cancellationToken = default) => Task.CompletedTask;

		public byte[] SerializeToUtf8Bytes<T>(T obj, bool serializeNulls = false) => [];
	}
}
