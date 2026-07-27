using Norse.Abstractions.Contracts;
using Norse.Abstractions.Web.Server.Mediator;
using Norse.Primitives;

namespace Norse.Abstractions.Web.Server.Tests.Mediator;

public sealed class IRequestHandlerTests
{
	[Fact]
	async Task Handle_works_for_a_request_implementing_IRequest()
	{
		IRequestHandler<EchoRequest, bool> handler = new EchoHandler();

		var result = await handler.Handle(new EchoRequest(), TestContext.Current.CancellationToken);

		result.TryGetValue(out Success<bool> success).ShouldBeTrue();
		success.Value.ShouldBeTrue();
	}

	sealed record EchoRequest : IQueryRequest<bool>;

	sealed class EchoHandler : IRequestHandler<EchoRequest, bool>
	{
		public ValueTask<Outcome<bool>> Handle(EchoRequest request, CancellationToken cancellationToken) =>
			ValueTask.FromResult(Outcome<bool>.Ok(true));
	}
}
