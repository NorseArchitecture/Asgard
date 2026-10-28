using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Contracts;
using Norse.Primitives;

namespace Norse.Abstractions.Components.Tests;

public sealed class FormValidationGateTests : BunitContext
{
	// bUnit throws MissingRendererInfoException on any RendererInfo read until one is set, and the
	// base reads it on every submit. Set after the validator is registered: setting it creates the
	// renderer, which locks the service provider.
	static RendererInfo Interactive =>
		new("Server", true);

	[Fact]
	async Task An_invalid_model_never_reaches_the_service()
	{
		var probe = Arrange(new Model(), TimeSpan.Zero);

		var submitted = await probe.Instance.Submit();

		submitted.ShouldBeFalse();
		probe.Instance.Calls.ShouldBe(0);
		probe.Instance.Context.GetValidationMessages().ShouldNotBeEmpty();
	}

	[Fact]
	async Task A_valid_model_reaches_the_service_once()
	{
		var probe = Arrange(new Model { Password = "aaaaaaaa" }, TimeSpan.Zero);

		var submitted = await probe.Instance.Submit();

		submitted.ShouldBeTrue();
		probe.Instance.Calls.ShouldBe(1);
	}

	[Fact]
	async Task A_second_submit_during_async_validation_is_rejected()
	{
		// Blazilla stashes the pending validation task in one EditContext.Properties slot, so
		// overlapping calls race it. IsSubmitting opens before validation, not before dispatch.
		// Calls alone is not deterministic proof: under the bug this guards (validation moved ahead
		// of IsSubmitting = true) both submits reach dispatch, and whether the second is caught
		// downstream depends on continuation ordering. Counting async-rule invocations is the
		// deterministic signal — correct placement runs the rule exactly once, because the second
		// submit returns at the IsSubmitting check before any await.
		ModelValidator validator = new(TimeSpan.FromMilliseconds(300));
		Services.AddScoped<IValidator<Model>>(_ => validator);
		SetRendererInfo(Interactive);
		var probe = Render<Probe>(parameters =>
			parameters.Add(p => p.Request, new Model { Password = "aaaaaaaa" }));

		var first = probe.Instance.Submit();
		var second = probe.Instance.Submit();
		var submitted = await Task.WhenAll(first, second);

		submitted.ShouldBe([true, false]);
		probe.Instance.Calls.ShouldBe(1);
		validator.Validations.ShouldBe(1);
	}

	[Fact]
	async Task An_async_rule_failure_is_awaited_and_blocks_dispatch()
	{
		// The whole reason AsyncMode is fixed on: a validator with no failing sync rule at all, only
		// an async rule that fails, must still be awaited to completion and gate dispatch.
		Services.AddScoped<IValidator<Model>>(_ => new AsyncFailingModelValidator());
		SetRendererInfo(Interactive);
		var probe = Render<Probe>(parameters => parameters.Add(p => p.Request, new Model()));

		var submitted = await probe.Instance.Submit();

		submitted.ShouldBeFalse();
		probe.Instance.Calls.ShouldBe(0);
		probe.Instance.Context.GetValidationMessages().ShouldNotBeEmpty();
	}

	[Fact]
	async Task A_sync_only_validator_still_gates_under_the_fixed_async_mode()
	{
		// The claim the one-shape design rests on: Login and CountryLookup carry no async rules, and
		// must gate identically without their authors choosing anything.
		Services.AddScoped<IValidator<Model>>(_ => new SyncModelValidator());
		SetRendererInfo(Interactive);
		var probe = Render<Probe>(parameters => parameters.Add(p => p.Request, new Model()));

		var submitted = await probe.Instance.Submit();

		submitted.ShouldBeFalse();
		probe.Instance.Calls.ShouldBe(0);
		probe.Instance.Context.GetValidationMessages().ShouldNotBeEmpty();
	}

	[Fact]
	async Task Disposal_during_async_validation_cancels_the_dispatch()
	{
		// The user navigates away while an async rule is still in flight. AsyncComponentBase allocates
		// its token source lazily, so a token first read at the dispatch site would be a brand-new,
		// uncanceled one — and the form would call the service on behalf of a component that no longer
		// exists. Reading the token before the validation await is what makes disposal reach this call.
		Services.AddScoped<IValidator<Model>>(_ => new ModelValidator(TimeSpan.FromMilliseconds(300)));
		SetRendererInfo(Interactive);
		// Held across the disposal: bUnit refuses Instance access once the component leaves the render
		// tree, and the assertions are about what the component did after that point.
		var probe = Render<Probe>(parameters =>
			parameters.Add(p => p.Request, new Model { Password = "aaaaaaaa" })).Instance;

		var submit = probe.Submit();
		await DisposeComponentsAsync();
		var submitted = await submit;

		submitted.ShouldBeFalse();
		probe.Calls.ShouldBe(0);
	}

	[Fact]
	async Task A_static_render_renders_the_retry_message_instead_of_dispatching()
	{
		// The prerendered form a user submits before Blazor boots: valid-looking input or not, the
		// post carries the initial model, so the form refuses on the page rather than calling out.
		Services.AddScoped<IValidator<Model>>(_ => new ModelValidator(TimeSpan.Zero));
		SetRendererInfo(new("Static", false));
		var probe = Render<Probe>(parameters => parameters
			.Add(p => p.Request, new Model { Password = "aaaaaaaa" })
			.Add(p => p.WithSummary, true));

		var submitted = await probe.InvokeAsync(probe.Instance.Submit);

		submitted.ShouldBeFalse();
		probe.Instance.Calls.ShouldBe(0);
		probe.Find(".validation-message").TextContent.ShouldBe("This page hasn't finished loading — try again.");
	}

	IRenderedComponent<Probe> Arrange(Model model, TimeSpan roundTrip)
	{
		Services.AddScoped<IValidator<Model>>(_ => new ModelValidator(roundTrip));
		SetRendererInfo(Interactive);
		return Render<Probe>(parameters => parameters.Add(p => p.Request, model));
	}

	sealed record Model
	{
		public string Password { get; init; } = "";
	}

	sealed class ModelValidator : AbstractValidator<Model>
	{
		int _validations;

		internal int Validations =>
			_validations;

		public ModelValidator(TimeSpan roundTrip) =>
			RuleFor(model => model.Password)
				.MinimumLength(8)
				.MustAsync(async (_, cancellationToken) =>
				{
					Interlocked.Increment(ref _validations);
					await Task.Delay(roundTrip, cancellationToken);
					return true;
				});
	}

	sealed class SyncModelValidator : AbstractValidator<Model>
	{
		public SyncModelValidator() =>
			RuleFor(model => model.Password)
				.NotEmpty()
				.MinimumLength(8);
	}

	sealed class AsyncFailingModelValidator : AbstractValidator<Model>
	{
		public AsyncFailingModelValidator() =>
			RuleFor(model => model.Password)
				.MustAsync(async (_, cancellationToken) =>
				{
					await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
					return false;
				});
	}

	sealed class Probe : OutcomeFormComponentBase
	{
		[Parameter]
		public Model Request { get; set; } = new();

		// Opt-in: the summary re-renders on validation changes, which the facts that submit off the
		// dispatcher cannot allow.
		[Parameter]
		public bool WithSummary { get; set; }

		internal int Calls { get; private set; }

		internal EditContext Context =>
			EditContextFor(Request);

		internal Task<bool> Submit() =>
			SubmitAsync(Context, _ =>
			{
				Calls++;
				return Task.FromResult<Outcome<Ack>>(new Success<Ack>(new()));
			}, _ => { });

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<EditForm>(0);
			builder.AddAttribute(1, nameof(EditForm.EditContext), Context);
			builder.AddAttribute(2, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => child =>
			{
				child.OpenComponent<FormValidator>(0);
				child.CloseComponent();
				if (!WithSummary)
					return;
				child.OpenComponent<ValidationSummary>(1);
				child.CloseComponent();
			}));
			builder.CloseComponent();
		}

		internal sealed record Ack;
	}
}
