namespace Norse.Abstractions.Contracts;

/// <summary>
/// The neutral marker every mediator-dispatched request implements, via one of its two derived
/// markers — <see cref="ICommandRequest{TResponse}"/> or <see cref="IQueryRequest{TResponse}"/>.
/// <typeparamref name="TResponse"/> is the handler's <b>payload</b> type; the pipeline wraps it in
/// <see cref="Outcome{T}"/> — request types never name the envelope.
/// </summary>
/// <typeparam name="TResponse">The success payload the request's handler produces.</typeparam>
public interface IRequest<TResponse> where TResponse : notnull;
