using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// The closed-generic dispatch entry: resolves <see cref="IRequestHandler{TRequest,TResponse}"/>
/// and every <see cref="IBehavior{TRequest,TResponse}"/> from the scoped provider and folds them
/// around the handler, <b>first-registered outermost</b> — registration order in
/// <c>AddNorsePipeline()</c> is the chain order, and it is law (spec §2.2). A missing handler
/// registration fails loudly here (<see cref="ServiceProviderServiceExtensions.GetRequiredService{T}(IServiceProvider)"/>),
/// never silently no-ops. Stateless; registered as a singleton by generated code.
/// </summary>
public sealed class SenderDispatch<TRequest, TResponse> : ISenderDispatch<TResponse>
	where TRequest : IRequest<TResponse>
	where TResponse : notnull
{
	/// <inheritdoc />
	public Type RequestType =>
		typeof(TRequest);

	/// <inheritdoc />
	public ValueTask<Outcome<TResponse>> Dispatch(IServiceProvider services, IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
		var typed = (TRequest)request;
		BehaviorDelegate<TResponse> next = () => handler.Handle(typed, cancellationToken);
		foreach (var behavior in services.GetServices<IBehavior<TRequest, TResponse>>().Reverse())
		{
			var current = next;
			next = () => behavior.Handle(typed, current, cancellationToken);
		}

		return next();
	}
}
