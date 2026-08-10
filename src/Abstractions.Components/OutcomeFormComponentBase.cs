using Blazilla;
using Microsoft.AspNetCore.Components.Forms;
using Norse.Abstractions.Contracts;
using Norse.Abstractions.Components.ServerValidation;
using Norse.Primitives;

namespace Norse.Abstractions.Components;

/// <summary>
///     The pit-of-success submit seam: pages hand
///     <see cref="SubmitAsync{T}(EditContext, Func{CancellationToken, Task{Outcome{T}}}, Action{T})" />
///     the call and the success continuation, and the <see cref="Outcome{T}" /> error story is handled where it
///     cannot be forgotten — <c>Failed</c> renders through
///     <see cref="EditContextServerErrorsExtensions.ApplyServerErrors" />,
///     success clears prior server errors before the continuation runs. Dispatch is gated, not just routed: the
///     attached <see cref="FormValidator" /> pass runs first, and an invalid model never reaches <c>call</c> — no
///     page needs to validate on its own. Total over the <see cref="Outcome{T}" /> domain
///     only: exceptions (a throwing transport, a throwing continuation) propagate to the circuit's error boundary
///     deliberately — swallowing them here would be a silent fallback.
/// </summary>
public abstract class OutcomeFormComponentBase : AsyncComponentBase
{
	const string BufferSuffix = "Input";

	EditContext? _editContext;

	/// <summary>True while a submit is in flight — bind to the submit button's <c>Disabled</c> state.</summary>
	protected bool IsSubmitting { get; private set; }

	/// <summary>
	///     Creates (once) the form's <see cref="EditContext" /> with the stamped-request
	///     field-identity mechanic wired in: the form binds raw buffers (<c>XInput</c>) while
	///     validation rules register on the stamps (<c>X</c>), and Blazilla's field-change pass
	///     selects rules by the changed member's name — so every buffer change is echoed as its
	///     stamp's change, making the stamp's rules run on blur. Mechanics without presentation,
	///     so it lives here, not in markup: bind the form as
	///     <c>&lt;EditForm EditContext="EditContextFor(_request)"&gt;</c>. The convention is the
	///     contract: a buffer is always its stamp's name + <c>Input</c>; the future request-buffer
	///     source generator owns the mapping end to end, and until then this is the one place it
	///     lives at runtime. (Re-entrancy-safe by construction: the echoed field never carries the
	///     buffer suffix, so the echo never echoes. No unsubscribe needed — the context and the
	///     page share a lifetime.)
	/// </summary>
	/// <param name="request">The wire request the form binds.</param>
	/// <returns>The page's one <see cref="EditContext" /> — the same instance on every render.</returns>
	protected EditContext EditContextFor(object request)
	{
		if (_editContext is not null)
			return _editContext;
		ArgumentNullException.ThrowIfNull(request);
		EditContext editContext = new(request);
		editContext.OnFieldChanged += static (sender, e) =>
		{
			var name = e.FieldIdentifier.FieldName;
			if (name.Length > BufferSuffix.Length && name.EndsWith(BufferSuffix, StringComparison.Ordinal))
				((EditContext)sender!).NotifyFieldChanged(new FieldIdentifier(e.FieldIdentifier.Model,
					name[..^BufferSuffix.Length]));
		};
		return _editContext = editContext;
	}

	/// <summary>Synchronous-continuation convenience over the <see cref="Func{T, Task}" /> overload.</summary>
	protected Task<bool> SubmitAsync<T>(EditContext editContext, Func<CancellationToken, Task<Outcome<T>>> call,
		Action<T> onSuccess)
		where T : notnull =>
		SubmitAsync(editContext, call, value =>
		{
			onSuccess(value);
			return Task.CompletedTask;
		});

	/// <summary>
	///     Validates <paramref name="editContext" /> against the <see cref="FormValidator" /> pass attached to it
	///     and returns without dispatching when invalid — neither <paramref name="call" /> nor
	///     <paramref name="onSuccess" /> runs for an invalid model. On a valid model, dispatches
	///     <paramref name="call" /> and routes its <see cref="Outcome{T}" />: failure into the form, success into
	///     <paramref name="onSuccess" />.
	/// </summary>
	/// <returns>
	///     <c>true</c> only when <paramref name="onSuccess" /> ran to completion. <c>false</c> covers every way the
	///     form still has the user's attention — the model failed validation, a submit was already in flight, or the
	///     call came back <c>Failed</c> and the problem is now rendered on the form. Callers that keep their own
	///     status UI (a banner to leave standing, a scroll position to hold) branch on this rather than assuming a
	///     completed <see cref="Task" /> means the submit landed.
	/// </returns>
	protected async Task<bool> SubmitAsync<T>(EditContext editContext, Func<CancellationToken, Task<Outcome<T>>> call,
		Func<T, Task> onSuccess)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(editContext);
		// Fail-loud, not a style check: a form bound Model="..." instead of
		// EditContextFor(request) silently loses the stamped-request blur mechanic — the exact
		// forgettable-markup failure folding the mechanic into this base exists to prevent.
		if (!ReferenceEquals(editContext, _editContext))
			throw new InvalidOperationException(
				"Bind the form as <EditForm EditContext=\"EditContextFor(_request)\"> — Model binding bypasses the stamped-request field mechanic.");
		// Checked before dispatch, not inferred from the result: a form with no validator attached
		// validates to true with zero messages, identical to a genuinely valid one.
		if (!editContext.Properties.TryGetValue(FormProperties.ValidatorAttached, out var attached)
			|| attached is not true)
			throw new InvalidOperationException(
				"No <FormValidator/> in this form — validation would pass vacuously.");
		if (IsSubmitting)
			return false;

		IsSubmitting = true;
		try
		{
			// CA2007 deliberately suppressed, not worked around: component code must resume on the
			// renderer's sync context, so ConfigureAwait(false) here would be a correctness bug, not
			// a style nit. See the class remarks.
#pragma warning disable CA2007
			// Static-invocation style is required, not preferred: .NET 11's instance
			// EditContext.ValidateAsync(CancellationToken) shadows Blazilla's extension, and
			// extension-style binding silently reports every form valid. See the plan's dispute.
			if (!await EditContextExtensions.ValidateAsync(editContext))
				return false;
			var outcome = await call(CancellationToken);
			switch (outcome)
			{
				case Success<T>(var value):
					editContext.ClearServerErrors();
					await onSuccess(value);
					return true;
				case Failed(var problem):
					editContext.ApplyServerErrors(problem);
					break;
			}
#pragma warning restore CA2007
			return false;
		}
		finally
		{
			IsSubmitting = false;
		}
	}
}
