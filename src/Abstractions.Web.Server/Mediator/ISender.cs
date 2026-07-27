using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// Dispatches a request through the composed behavior chain to its handler — the platform's
/// hand-rolled, MediatR-familiar seam (spec §2.2). One implementation lives in Midgard; callers
/// (service implementations, never components) constructor-inject this and stay channel-dumb.
/// </summary>
public interface ISender
{
	/// <summary>Sends the request through the pipeline and returns the enveloped payload.</summary>
	ValueTask<Outcome<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
		where TResponse : notnull;
}
