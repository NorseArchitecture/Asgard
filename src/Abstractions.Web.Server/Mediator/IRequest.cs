namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// The neutral marker every mediator-dispatched request implements, via one of its two derived
/// markers — <see cref="ICommandRequest{TResponse}"/> or <see cref="IQueryRequest{TResponse}"/>.
/// <typeparamref name="TResponse"/> is the handler's <b>payload</b> type; the pipeline wraps it in
/// <see cref="Norse.Abstractions.Contracts.Outcome{T}"/> — request types never name the envelope.
/// Deliberately server-only — living in <c>Norse.Abstractions.Web.Server</c> rather than
/// <c>Norse.Abstractions.Contracts</c> means a WASM-shipped wire assembly cannot even reference
/// this interface, let alone implement it; that structural impossibility is the actual enforcement
/// of wire purity, not a naming convention someone could accidentally violate.
/// </summary>
/// <typeparam name="TResponse">The success payload the request's handler produces.</typeparam>
public interface IRequest<TResponse> where TResponse : notnull;
