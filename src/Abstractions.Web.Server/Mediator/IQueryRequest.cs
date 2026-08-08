namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
///     A side-effect-free read request. See <see cref="ICommandRequest{TResponse}" /> for why the split
///     exists and <see cref="IRequest{TResponse}" /> for why this family is server-only.
/// </summary>
/// <typeparam name="TResponse">The success payload the request's handler produces.</typeparam>
public interface IQueryRequest<TResponse> : IRequest<TResponse> where TResponse : notnull;
