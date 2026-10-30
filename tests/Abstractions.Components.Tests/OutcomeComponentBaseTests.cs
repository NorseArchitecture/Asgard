using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Components.Tests;

// A BunitContext for the reauthentication facts, which need the injected NavigationManager; the
// dispatch-state facts still new their Harness up directly, since none of them navigates.
public sealed class OutcomeComponentBaseTests : BunitContext
{
	const string Landing = "/Account/Login?returnUrl=%2FAccount%2FManage%2FPasskeys";

	public static TheoryData<string> NonLocalNextUrls =>
	[
		"//evil.example",
		"/\t/evil.example",
		"/\n/evil.example",
	];

	public static TheoryData<ErrorCategory, string?> OtherFailures => new()
	{
		{ ErrorCategory.Forbidden, null },
		{ ErrorCategory.Forbidden, "some-other-code" },
		{ ErrorCategory.Unauthorized, NorseErrorCodes.ReauthenticationRequired },
		{ ErrorCategory.Fault, null },
	};

	BunitNavigationManager Navigation =>
		Services.GetRequiredService<BunitNavigationManager>();

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

	static Problem Reauthentication(string? nextUrl) =>
		new() { Category = ErrorCategory.Forbidden, Code = NorseErrorCodes.ReauthenticationRequired, NextUrl = nextUrl };

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
	async Task A_dispatch_on_an_already_disposed_component_never_invokes_the_call()
	{
		var called = false;
		using Harness harness = new();
		harness.Dispose();

		await harness.Dispatch(_ =>
		{
			called = true;
			return Task.FromResult(Success());
		}, _ => { });

		called.ShouldBeFalse();
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

	[Fact]
	async Task A_reauthentication_failure_with_a_local_next_url_navigates_to_it()
	{
		var harness = Render<Harness>().Instance;

		await harness.Dispatch(_ => Task.FromResult<Outcome<FakeResult>>(new Failed(Reauthentication(Landing))), _ => { });

		var entry = Navigation.History.ShouldHaveSingleItem();
		entry.Uri.ShouldBe(Landing);
		entry.Options.ForceLoad.ShouldBeFalse();
		harness.CapturedProblem.ShouldBeNull();
	}

	[Theory]
	[MemberData(nameof(NonLocalNextUrls))]
	async Task A_reauthentication_failure_with_a_non_local_next_url_renders_the_problem(string nextUrl)
	{
		var harness = Render<Harness>().Instance;
		var problem = Reauthentication(nextUrl);

		await harness.Dispatch(_ => Task.FromResult<Outcome<FakeResult>>(new Failed(problem)), _ => { });

		Navigation.History.ShouldBeEmpty();
		harness.CapturedProblem.ShouldBeSameAs(problem);
	}

	[Theory]
	[MemberData(nameof(OtherFailures))]
	async Task A_next_url_on_any_other_failure_never_navigates(ErrorCategory category, string? code)
	{
		var harness = Render<Harness>().Instance;
		Problem problem = new() { Category = category, Code = code, NextUrl = Landing };

		await harness.Dispatch(_ => Task.FromResult<Outcome<FakeResult>>(new Failed(problem)), _ => { });

		Navigation.History.ShouldBeEmpty();
		harness.CapturedProblem.ShouldBeSameAs(problem);
	}

	[Fact]
	async Task A_failure_without_next_url_renders_the_problem_as_today()
	{
		var harness = Render<Harness>().Instance;
		var problem = Reauthentication(null);

		await harness.Dispatch(_ => Task.FromResult<Outcome<FakeResult>>(new Failed(problem)), _ => { });

		Navigation.History.ShouldBeEmpty();
		harness.CapturedProblem.ShouldBeSameAs(problem);
	}
}
