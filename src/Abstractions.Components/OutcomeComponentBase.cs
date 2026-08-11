using Norse.Abstractions.Contracts;
using Norse.Primitives;

namespace Norse.Abstractions.Components;

/// <summary>
///     The load-time sibling of <see cref="OutcomeFormComponentBase" />: a page whose
///     outcome-consuming operation has no form declares only the success continuation, and the
///     <see cref="Outcome{T}" /> failure story is handled where it cannot be forgotten —
///     <c>Failed</c> lands in <see cref="Problem" /> for the page's markup to render. Total over the
///     <see cref="Outcome{T}" /> domain only: exceptions (a throwing transport, a throwing
///     continuation) propagate to the circuit's error boundary deliberately — swallowing them here
///     would be a silent fallback.
/// </summary>
public abstract class OutcomeComponentBase : AsyncComponentBase
{
	/// <summary>The failure of the last dispatch, rendered by the page's markup. Null until a dispatch fails.</summary>
	protected Problem? Problem { get; private set; }

	/// <summary>True while a dispatch is in flight — bind to the trigger's disabled state.</summary>
	protected bool IsDispatching { get; private set; }

	/// <summary>Synchronous-continuation convenience over the <see cref="Func{T, Task}" /> overload.</summary>
	protected Task DispatchAsync<T>(Func<CancellationToken, Task<Outcome<T>>> call, Action<T> onSuccess)
		where T : notnull =>
		DispatchAsync(call, value =>
		{
			onSuccess(value);
			return Task.CompletedTask;
		});

	/// <summary>
	///     Dispatches <paramref name="call" /> and routes its <see cref="Outcome{T}" />: failure into
	///     <see cref="Problem" />, success into <paramref name="onSuccess" />. The state rules are law:
	///     a dispatch clears the prior <see cref="Problem" /> when it starts; an overlapping dispatch
	///     returns without dispatching; and disposal during the call writes no result state — no
	///     <see cref="Problem" />, no continuation; the component is gone, so there is nothing to
	///     render onto. The in-flight guard still releases in <c>finally</c> — re-entrancy
	///     bookkeeping, deliberately exempt from the no-result-state rule.
	/// </summary>
	protected async Task DispatchAsync<T>(Func<CancellationToken, Task<Outcome<T>>> call, Func<T, Task> onSuccess)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(call);
		ArgumentNullException.ThrowIfNull(onSuccess);
		if (IsDispatching)
			return;

		IsDispatching = true;
		try
		{
			Problem = null;
			// Read once, before the first await, so the token the dispatch runs under is the same one
			// disposal cancels.
			var cancellationToken = CancellationToken;
			// Not a silent fallback: the component is gone, so there is no page left to render a
			// problem onto and no continuation worth running. Dispatching here would be an
			// unrequested server write (e.g. the intended logout) on behalf of a user who navigated
			// away — the exact gap SubmitAsync already guards against before its own service call.
			if (cancellationToken.IsCancellationRequested)
				return;
			// CA2007 deliberately suppressed, not worked around: component code must resume on the
			// renderer's sync context, so ConfigureAwait(false) here would be a correctness bug, not
			// a style nit.
#pragma warning disable CA2007
			var outcome = await call(cancellationToken);
			// Checked again after the await: disposal during the call runs no continuation and
			// writes no result state (the guard below still releases — bookkeeping, not results).
			if (cancellationToken.IsCancellationRequested)
				return;
			switch (outcome)
			{
				case Success<T>(var value):
					await onSuccess(value);
					break;
				case Failed(var problem):
					Problem = problem;
					break;
			}
#pragma warning restore CA2007
		}
		finally
		{
			IsDispatching = false;
		}
	}
}
