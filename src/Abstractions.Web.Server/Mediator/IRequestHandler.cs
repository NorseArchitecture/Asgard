namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// Handles a single <see cref="ICommandRequest{TResponse}"/>. Every gRPC <c>[OperationContract]</c>
/// method forwards to exactly one of these — no business logic lives in the gRPC service class
/// (<c>Heimdall/specs/2026-07-13-authn-identity-split-design.md</c> §0/§3).
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
	where TRequest : ICommandRequest<TResponse>
{
	/// <summary>
	/// Handles the given request and returns a response of type <typeparamref name="TResponse"/>.
	/// </summary>
	ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
