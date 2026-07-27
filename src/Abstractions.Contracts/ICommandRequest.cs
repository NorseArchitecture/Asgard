namespace Norse.Abstractions.Contracts;

/// <summary>
/// A state-changing request. The command/query split carries no behavioral difference in v1 — it
/// exists so a future behavior (a transaction behavior being the obvious tenant) can bind to one
/// side only without re-marking every request on the platform.
/// </summary>
/// <typeparam name="TResponse">The success payload the request's handler produces.</typeparam>
public interface ICommandRequest<TResponse> : IRequest<TResponse> where TResponse : notnull;
