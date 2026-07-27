namespace Norse.Abstractions.Contracts.Tests;

public sealed class RequestMarkerTests
{
	sealed record FakePayload;
	sealed record FakeCommand : ICommandRequest<FakePayload>;
	sealed record FakeQuery : IQueryRequest<FakePayload>;

	[Fact]
	void Command_and_query_markers_both_derive_from_the_neutral_request_marker()
	{
		typeof(IRequest<FakePayload>).IsAssignableFrom(typeof(FakeCommand)).ShouldBeTrue();
		typeof(IRequest<FakePayload>).IsAssignableFrom(typeof(FakeQuery)).ShouldBeTrue();
	}
}
