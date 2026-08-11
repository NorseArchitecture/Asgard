using Microsoft.AspNetCore.Components;

namespace Norse.Abstractions.Components;

/// <summary>
///     A <see cref="ComponentBase" /> that owns a cancellation token scoped to its own lifetime, so a
///     component's async work can cooperatively cancel when the component is disposed (e.g. the user
///     navigates away mid-request) instead of touching UI state after teardown. Purely opt-in — a
///     derived component that never reads <see cref="CancellationToken" /> pays no cost.
/// </summary>
public abstract class AsyncComponentBase : ComponentBase, IDisposable
{
	CancellationTokenSource? _cts;
	bool _disposed;

	/// <summary>
	///     Gets a token that is canceled when this component is disposed. Lazily allocates its backing
	///     <see cref="CancellationTokenSource" /> on first access, so components that never request it
	///     never pay for it. After disposal the token is already canceled — lazy allocation must not
	///     mean a torn-down component can mint a live token and keep working, which is what a first
	///     access arriving after <see cref="Dispose" /> would otherwise do.
	/// </summary>
	protected CancellationToken CancellationToken =>
		_disposed ? new(true) : (_cts ??= new()).Token;

	/// <summary>
	///     Cancels and disposes the token returned by <see cref="CancellationToken" />, if it was ever
	///     requested. A derived component with its own disposal needs must override this and call
	///     <c>base.Dispose()</c>.
	/// </summary>
	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
		_disposed = true;
		if (_cts is null)
			return;
		_cts.Cancel();
		_cts.Dispose();
		_cts = null;
	}
}
