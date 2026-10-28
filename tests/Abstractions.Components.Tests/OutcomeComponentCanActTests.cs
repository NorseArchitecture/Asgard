using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Components.Tests;

// A BunitContext of its own, not a fact on OutcomeComponentBaseTests: that class news its Harness up
// directly, and CanAct reads RendererInfo, which only a rendered component has. SetRendererInfo touches
// the Renderer, sealing the service collection, so it is always the last line of setup.
public sealed class OutcomeComponentCanActTests : BunitContext
{
	[Fact]
	void An_interactive_idle_component_can_act()
	{
		SetRendererInfo(new RendererInfo("Server", isInteractive: true));
		var cut = Render<Harness>();

		cut.Find("button").HasAttribute("disabled").ShouldBeFalse();
	}

	[Fact]
	void A_static_render_cannot_act()
	{
		// A prerendered click trigger renders before Blazor boots, and a click on it does nothing (for
		// good if the interactive runtime never starts): it offers nothing to click.
		SetRendererInfo(new RendererInfo("Static", isInteractive: false));
		var cut = Render<Harness>();

		cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
	}

	[Fact]
	void An_in_flight_dispatch_cannot_act()
	{
		TaskCompletionSource<Outcome<Ack>> pending = new();
		SetRendererInfo(new RendererInfo("Server", isInteractive: true));
		var cut = Render<Harness>(parameters => parameters.Add(p => p.Call, _ => pending.Task));

		cut.Find("button").Click();

		cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
		pending.SetResult(Outcome<Ack>.Ok(new()));
		cut.WaitForAssertion(() => cut.Find("button").HasAttribute("disabled").ShouldBeFalse());
	}

	// Guard, green before and after CanAct: loaders are not gated. A static render still runs its
	// load-time dispatch — CanAct gates click triggers only, never DispatchAsync itself.
	[Fact]
	async Task A_static_render_still_dispatches_a_loader()
	{
		var calls = 0;
		SetRendererInfo(new RendererInfo("Static", isInteractive: false));
		var cut = Render<Harness>(parameters => parameters.Add(p => p.Call, _ =>
		{
			calls++;
			return Task.FromResult(Outcome<Ack>.Ok(new()));
		}));

		await cut.InvokeAsync(cut.Instance.Dispatch);

		calls.ShouldBe(1);
	}

	internal sealed record Ack;

	sealed class Harness : OutcomeComponentBase
	{
		[Parameter]
		public Func<CancellationToken, Task<Outcome<Ack>>> Call { get; set; } =
			_ => Task.FromResult(Outcome<Ack>.Ok(new()));

		internal Task Dispatch() =>
			DispatchAsync(Call, _ => { });

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "button");
			builder.AddAttribute(1, "disabled", !CanAct);
			builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, Dispatch));
			builder.CloseElement();
		}
	}
}
