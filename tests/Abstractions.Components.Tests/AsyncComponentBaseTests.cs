namespace Norse.Abstractions.Components.Tests;

public sealed class AsyncComponentBaseTests
{
	[Fact]
	void A_token_taken_before_disposal_is_canceled_by_disposal()
	{
		Probe probe = new();
		var token = probe.Token;

		token.IsCancellationRequested.ShouldBeFalse();
		probe.Dispose();
		token.IsCancellationRequested.ShouldBeTrue();
	}

	[Fact]
	void A_token_taken_after_disposal_is_already_canceled()
	{
		// The lazy allocation is the trap: a component disposed before its token was ever requested
		// has no source to cancel, so a first read arriving late would otherwise mint a live token
		// and let torn-down work carry on as if nothing happened.
		Probe probe = new();
		probe.Dispose();

		probe.Token.IsCancellationRequested.ShouldBeTrue();
	}

	[Fact]
	void Disposal_is_idempotent()
	{
		Probe probe = new();
		_ = probe.Token;

		probe.Dispose();

		Should.NotThrow(probe.Dispose);
	}

	[Fact]
	void The_same_token_is_handed_out_on_every_read()
	{
		using Probe probe = new();

		probe.Token.ShouldBe(probe.Token);
	}

	sealed class Probe : AsyncComponentBase
	{
		internal CancellationToken Token =>
			CancellationToken;
	}
}
