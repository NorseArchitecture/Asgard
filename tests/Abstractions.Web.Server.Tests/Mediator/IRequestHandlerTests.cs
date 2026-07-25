using Norse.Abstractions.Web.Server.Mediator;

namespace Norse.Abstractions.Web.Server.Tests.Mediator;

public sealed class IRequestHandlerTests
{
	[Fact]
	async Task Handle_works_for_a_request_type_that_implements_nothing()
	{
		IRequestHandler<UnconstrainedRequest, bool> handler = new EchoHandler();

		var result = await handler.Handle(new UnconstrainedRequest(), TestContext.Current.CancellationToken);

		result.ShouldBeTrue();
	}

	sealed record UnconstrainedRequest;

	sealed class EchoHandler : IRequestHandler<UnconstrainedRequest, bool>
	{
		public ValueTask<bool> Handle(UnconstrainedRequest request, CancellationToken cancellationToken) =>
			ValueTask.FromResult(true);
	}
}
