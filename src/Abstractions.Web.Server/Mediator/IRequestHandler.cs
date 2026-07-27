namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// Handles a single request and returns a response of type <typeparamref name="TResponse"/>. Deliberately unconstrained.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
{
	/// <summary>
	/// Handles the given request and returns a response of type <typeparamref name="TResponse"/>.
	/// </summary>
	ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}
