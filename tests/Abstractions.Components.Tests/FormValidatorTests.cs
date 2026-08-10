using System.Reflection;
using Blazilla;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Norse.Abstractions.Components.Tests;

public sealed class FormValidatorTests : BunitContext
{
	[Fact]
	void Rendering_inside_a_form_stamps_the_marker()
	{
		EditContext context = new(new object());

		Render<CascadingValue<EditContext>>(parameters => parameters
			.Add(p => p.Value, context)
			.Add(p => p.IsFixed, true)
			.Add(p => p.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<FormValidator>(0);
				builder.CloseComponent();
			})));

		context.Properties.TryGetValue(FormProperties.ValidatorAttached, out var attached).ShouldBeTrue();
		attached.ShouldBe(true);
	}

	[Fact]
	void AsyncMode_renders_true_on_the_attached_FluentValidator()
	{
		EditContext context = new(new object());

		var host = Render<CascadingValue<EditContext>>(parameters => parameters
			.Add(p => p.Value, context)
			.Add(p => p.IsFixed, true)
			.Add(p => p.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<FormValidator>(0);
				builder.CloseComponent();
			})));

		host.FindComponent<FluentValidator>().Instance.AsyncMode.ShouldBeTrue();
	}

	[Fact]
	void AsyncMode_is_not_reachable_from_markup()
	{
		// AsyncMode=false against an async rule reports valid, then throws on a ThreadPool thread
		// out of Blazilla's async void handler. The trap is deleting the knob, not documenting it.
		var parameters = typeof(FormValidator)
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(property => property.GetCustomAttribute<ParameterAttribute>() is not null)
			.Select(property => property.Name);

		parameters.ShouldNotContain("AsyncMode");
	}

	[Fact]
	void Rendering_outside_a_form_is_rejected_loudly()
	{
		Should.Throw<InvalidOperationException>(() => Render<FormValidator>())
			.Message.ShouldContain("EditForm");
	}
}
