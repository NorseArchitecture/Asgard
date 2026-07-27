using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Contracts;
using Norse.Abstractions.Web.Server.Mediator;
using Norse.Primitives;

namespace Norse.Abstractions.Web.Server.Tests;

public sealed class SenderDispatchTests
{
	sealed record Ping : IQueryRequest<string>;

	sealed class PingHandler : IRequestHandler<Ping, string>
	{
		public ValueTask<Outcome<string>> Handle(Ping request, CancellationToken cancellationToken = default) =>
			ValueTask.FromResult(Outcome<string>.Ok("pong"));
	}

	sealed class TaggingBehavior(string tag, List<string> log) : IBehavior<Ping, string>
	{
		public async ValueTask<Outcome<string>> Handle(Ping request, BehaviorDelegate<string> next, CancellationToken cancellationToken = default)
		{
			log.Add($"{tag}:in");
			var outcome = await next();
			log.Add($"{tag}:out");
			return outcome;
		}
	}

	[Fact]
	async Task Folds_behaviors_first_registered_outermost_around_the_handler()
	{
		List<string> log = [];
		var services = new ServiceCollection()
			.AddScoped<IRequestHandler<Ping, string>, PingHandler>()
			.AddScoped<IBehavior<Ping, string>>(_ => new TaggingBehavior("first", log))
			.AddScoped<IBehavior<Ping, string>>(_ => new TaggingBehavior("second", log))
			.BuildServiceProvider();

		SenderDispatch<Ping, string> dispatch = new();
		var outcome = await dispatch.Dispatch(services, new Ping(), CancellationToken.None);

		outcome.TryGetValue(out Success<string> success).ShouldBeTrue();
		success.Value.ShouldBe("pong");
		log.ShouldBe(["first:in", "second:in", "second:out", "first:out"]);
	}

	[Fact]
	async Task Dispatches_straight_to_the_handler_when_no_behaviors_are_registered()
	{
		var services = new ServiceCollection()
			.AddScoped<IRequestHandler<Ping, string>, PingHandler>()
			.BuildServiceProvider();

		SenderDispatch<Ping, string> dispatch = new();
		var outcome = await dispatch.Dispatch(services, new Ping(), CancellationToken.None);

		outcome.TryGetValue(out Success<string> success).ShouldBeTrue();
		success.Value.ShouldBe("pong");
	}

	[Fact]
	async Task Fails_loudly_when_the_handler_registration_is_missing()
	{
		var services = new ServiceCollection().BuildServiceProvider();
		SenderDispatch<Ping, string> dispatch = new();
		await Should.ThrowAsync<InvalidOperationException>(async () =>
			await dispatch.Dispatch(services, new Ping(), CancellationToken.None));
	}
}
