namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// A state-changing request. The command/query split carries no behavioral difference in v1 — it
/// exists so a future behavior (a transaction behavior being the obvious tenant) can bind to one
/// side only without re-marking every request on the platform. Server-only, same as
/// <see cref="IRequest{TResponse}"/> — see its remark.
/// </summary>
/// <typeparam name="TResponse">The success payload the request's handler produces.</typeparam>
public interface ICommandRequest<TResponse> : IRequest<TResponse> where TResponse : notnull;
