using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Components.Tests;

public sealed class OutcomeComponentBaseTests
{
	sealed record FakeResult;

	sealed class Harness : OutcomeComponentBase
	{
		internal Problem? CapturedProblem =>
			Problem;

		internal bool Dispatching =>
			IsDispatching;

		internal Task Dispatch<T>(Func<CancellationToken, Task<Outcome<T>>> call, Action<T> onSuccess)
			where T : notnull =>
			DispatchAsync(call, onSuccess);
	}

	static Outcome<FakeResult> Success() =>
		Outcome<FakeResult>.Ok(new FakeResult());

	static Outcome<FakeResult> Failure() =>
		Outcome<FakeResult>.Err(ErrorCategory.Validation,
			new Dictionary<string, string[]> { [string.Empty] = ["nope"] });

	[Fact]
	async Task Success_invokes_the_continuation_and_leaves_no_problem()
	{
		var invoked = false;
		using Harness harness = new();

		await harness.Dispatch(_ => Task.FromResult(Success()), _ => invoked = true);

		invoked.ShouldBeTrue();
		harness.CapturedProblem.ShouldBeNull();
	}

	[Fact]
	async Task Failure_captures_the_problem_and_skips_the_continuation()
	{
		var invoked = false;
		using Harness harness = new();

		await harness.Dispatch(_ => Task.FromResult(Failure()), _ => invoked = true);

		invoked.ShouldBeFalse();
		harness.CapturedProblem.ShouldNotBeNull();
		harness.CapturedProblem.Category.ShouldBe(ErrorCategory.Validation);
	}

	[Fact]
	async Task A_new_dispatch_clears_the_prior_problem()
	{
		using Harness harness = new();
		await harness.Dispatch(_ => Task.FromResult(Failure()), _ => { });

		await harness.Dispatch(_ => Task.FromResult(Success()), _ => { });

		harness.CapturedProblem.ShouldBeNull();
	}

	[Fact]
	async Task An_overlapping_dispatch_returns_without_dispatching()
	{
		var calls = 0;
		TaskCompletionSource<Outcome<FakeResult>> pending = new();
		using Harness harness = new();
		var first = harness.Dispatch(_ =>
		{
			calls++;
			return pending.Task;
		}, _ => { });

		await harness.Dispatch(_ =>
		{
			calls++;
			return Task.FromResult(Success());
		}, _ => { });
		pending.SetResult(Success());
		await first;

		calls.ShouldBe(1);
	}

	[Fact]
	async Task Disposal_during_the_call_runs_no_continuation_and_writes_no_state()
	{
		var invoked = false;
		using Harness harness = new();

		await harness.Dispatch(_ =>
		{
			harness.Dispose();
			return Task.FromResult(Failure());
		}, _ => invoked = true);

		invoked.ShouldBeFalse();
		harness.CapturedProblem.ShouldBeNull();
	}

	[Fact]
	async Task A_throwing_continuation_propagates_and_releases_the_guard()
	{
		using Harness harness = new();

		await Should.ThrowAsync<InvalidOperationException>(() =>
			harness.Dispatch(_ => Task.FromResult(Success()),
				_ => throw new InvalidOperationException("boom")));

		harness.Dispatching.ShouldBeFalse();
	}
}
