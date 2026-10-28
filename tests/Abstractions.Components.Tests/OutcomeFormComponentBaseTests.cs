using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Norse.Abstractions.Contracts;
using Norse.Abstractions.Components.ServerValidation;
using Norse.Primitives;

namespace Norse.Abstractions.Components.Tests;

public sealed class OutcomeFormComponentBaseTests : BunitContext
{
	// bUnit throws MissingRendererInfoException on any RendererInfo read until one is set, and the
	// base reads it on every submit — so these start interactive, the state a form is in once Blazor
	// boots; the static-render facts override it. Set here because nothing registers services.
	public OutcomeFormComponentBaseTests() =>
		SetRendererInfo(new("Server", true));

	[Fact]
	async Task Success_invokes_the_continuation_and_clears_server_errors()
	{
		var harness = Render<Harness>().Instance;
		var context = harness.ContextFor(new object());
		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Stale."));
		var invoked = false;

		var submitted = await harness.Submit(context,
			_ => Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new())),
			_ => invoked = true);

		submitted.ShouldBeTrue();
		invoked.ShouldBeTrue();
		context.GetValidationMessages().ShouldBeEmpty();
	}

	[Fact]
	async Task Failure_applies_the_problem_and_skips_the_continuation()
	{
		var harness = Render<Harness>().Instance;
		var model = new object();
		var context = harness.ContextFor(model);
		var invoked = false;

		var submitted = await harness.Submit(context,
			_ => Task.FromResult<Outcome<FakeResult>>(
				new Failed(Problem.ModelError(ErrorCategory.LockedOut, "Locked."))),
			_ => invoked = true);

		// A dispatched call that came back Failed is still false: the form kept the user.
		submitted.ShouldBeFalse();
		invoked.ShouldBeFalse();
		context.GetValidationMessages(new FieldIdentifier(model, string.Empty)).ShouldBe(["Locked."]);
	}

	[Fact]
	async Task A_foreign_edit_context_is_rejected_loudly()
	{
		// Model="..." binding creates its own EditContext, silently bypassing the stamped-request
		// blur mechanic — the guard makes that a loud failure instead of a quiet UX regression.
		var harness = Render<Harness>().Instance;
		EditContext foreign = new(new object());

		await Should.ThrowAsync<InvalidOperationException>(
			harness.Submit(foreign, _ => Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new())),
				_ => { }));
	}

	[Fact]
	async Task An_overlapping_call_returns_without_dispatching()
	{
		var harness = Render<Harness>().Instance;
		var context = harness.ContextFor(new object());
		TaskCompletionSource<Outcome<FakeResult>> pending = new();
		var calls = 0;

		var first = harness.Submit(context, _ =>
		{
			calls++;
			return pending.Task;
		}, _ => { });
		var overlapped = await harness.Submit(context, _ =>
		{
			calls++;
			return pending.Task;
		}, _ => { });

		overlapped.ShouldBeFalse();
		calls.ShouldBe(1);
		harness.Submitting.ShouldBeTrue();
		pending.SetResult(new Success<FakeResult>(new()));
		(await first).ShouldBeTrue();
		harness.Submitting.ShouldBeFalse();
	}

	[Fact]
	async Task A_throwing_continuation_propagates_and_releases_the_guard()
	{
		var harness = Render<Harness>().Instance;
		var context = harness.ContextFor(new object());

		await Should.ThrowAsync<InvalidOperationException>(
			harness.Submit(context, _ => Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new())),
				_ => throw new InvalidOperationException("continuation bug")));

		harness.Submitting.ShouldBeFalse();
	}

	[Fact]
	async Task A_form_with_no_validator_is_rejected_loudly()
	{
		// A validator-less form and a valid form both validate to true with zero messages, so this
		// must be caught before dispatch — after the fact the two are indistinguishable.
		var harness = Render<Harness>().Instance;
		var context = harness.UnstampedContextFor(new object());
		var calls = 0;

		await Should.ThrowAsync<InvalidOperationException>(
			harness.Submit(context, _ =>
			{
				calls++;
				return Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new()));
			}, _ => { }));

		calls.ShouldBe(0);
	}

	[Fact]
	async Task Disposal_during_the_call_skips_the_continuation()
	{
		var invoked = false;
		var harness = Render<Harness>().Instance;
		var context = harness.ContextFor(new object());

		var submitted = await harness.Submit(context, _ =>
		{
			// Dispose mid-call: the token SubmitAsync captured before the await is now canceled,
			// but the call itself still completes successfully.
			harness.Dispose();
			return Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new()));
		}, _ => invoked = true);

		submitted.ShouldBeFalse();
		invoked.ShouldBeFalse();
	}

	[Fact]
	async Task A_static_render_never_dispatches_and_asks_the_user_to_retry()
	{
		// A prerendered form posted before Blazor boots carries the page's initial model, never the
		// user's input — dispatching it would report a false success or a misleading error.
		SetRendererInfo(new("Static", false));
		var harness = Render<Harness>().Instance;
		var model = new object();
		var context = harness.ContextFor(model);
		var calls = 0;

		var submitted = await harness.Submit(context, _ =>
		{
			calls++;
			return Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new()));
		}, _ => { });

		submitted.ShouldBeFalse();
		calls.ShouldBe(0);
		harness.Submitting.ShouldBeFalse();
		context.GetValidationMessages(new FieldIdentifier(model, string.Empty))
			.ShouldBe(["This page hasn't finished loading — try again."]);
	}

	[Fact]
	void An_interactive_idle_form_can_submit()
	{
		var harness = Render<Harness>().Instance;

		harness.Submittable.ShouldBeTrue();
	}

	[Fact]
	void A_static_render_cannot_submit()
	{
		SetRendererInfo(new("Static", false));
		var harness = Render<Harness>().Instance;

		harness.Submittable.ShouldBeFalse();
	}

	[Fact]
	async Task An_in_flight_submit_cannot_submit_again()
	{
		var harness = Render<Harness>().Instance;
		var context = harness.ContextFor(new object());
		TaskCompletionSource<Outcome<FakeResult>> pending = new();

		var submit = harness.Submit(context, _ => pending.Task, _ => { });

		harness.Submittable.ShouldBeFalse();
		pending.SetResult(new Success<FakeResult>(new()));
		(await submit).ShouldBeTrue();
		harness.Submittable.ShouldBeTrue();
	}

	sealed record FakeResult;

	sealed class Harness : OutcomeFormComponentBase
	{
		internal bool Submitting =>
			IsSubmitting;

		internal bool Submittable =>
			CanSubmit;

		internal Task<bool> Submit<T>(EditContext editContext, Func<CancellationToken, Task<Outcome<T>>> call,
			Action<T> onSuccess) where T : notnull =>
			SubmitAsync(editContext, call, onSuccess);

		internal EditContext ContextFor(object request)
		{
			var context = EditContextFor(request);
			context.Properties[FormProperties.ValidatorAttached] = true;
			return context;
		}

		internal EditContext UnstampedContextFor(object request) =>
			EditContextFor(request);
	}
}
