namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// Marker for a mediator-dispatched request whose handler produces a <typeparamref name="TResponse"/>.
/// </summary>
public interface ICommandRequest<TResponse>;
