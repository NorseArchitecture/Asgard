namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// Handles a single request and returns a response of type <typeparamref name="TResponse"/>. Deliberately
/// unconstrained — nothing in the platform dispatches through a generic sender yet, so requiring
/// <typeparamref name="TRequest"/> to implement <see cref="ICommandRequest{TResponse}"/> was dead weight
/// that only forced WASM-referenced wire types to reference this server-only assembly. Revisit once a real
/// generic dispatcher exists.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
{
	/// <summary>
	/// Handles the given request and returns a response of type <typeparamref name="TResponse"/>.
	/// </summary>
	ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
