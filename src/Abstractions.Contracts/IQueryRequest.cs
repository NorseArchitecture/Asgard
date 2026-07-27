namespace Norse.Abstractions.Contracts;

/// <summary>A side-effect-free read request. See <see cref="ICommandRequest{TResponse}"/> for why the split exists.</summary>
/// <typeparam name="TResponse">The success payload the request's handler produces.</typeparam>
public interface IQueryRequest<TResponse> : IRequest<TResponse> where TResponse : notnull;
