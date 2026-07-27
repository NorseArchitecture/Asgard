using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// Handles a single request, producing the payload wrapped in the platform envelope. The whole
/// chain — <see cref="ISender"/>, <see cref="IBehavior{TRequest,TResponse}"/>, this interface —
/// speaks one type algebra: <typeparamref name="TResponse"/> is the <b>payload</b>, the pipeline
/// owns the <see cref="Outcome{T}"/>. Handlers never validate, authorize, or catch-to-translate —
/// the behaviors composed around them by <c>AddNorsePipeline()</c> (Midgard) do.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
	where TRequest : IRequest<TResponse>
	where TResponse : notnull
{
	/// <summary>Handles the given request.</summary>
	ValueTask<Outcome<TResponse>> Handle(TRequest request, CancellationToken cancellationToken = default);
}
