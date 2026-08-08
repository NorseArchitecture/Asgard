namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
///     The server-sovereign wrapper a realm derives to give a wire request its mediator identity —
///     an <c>[Authorize]</c> policy, a unique handler binding — while carrying the original wire DTO
///     whole. Wire records stay pure: no mediator marker, no <c>[Authorize]</c>, nothing but the
///     <c>[DataContract]</c> shape the transport needs. The gRPC service hydrates one of these around
///     the wire request and <see cref="ISender" />s it;
///     <see cref="CommandRequestValidator{TCommand,TRequest,TResponse}" />
///     reaches through <see cref="Request" /> to validate it with the wire type's own validators, so
///     validation rules are declared exactly once — reused client-side (against the wire type
///     directly) and server-side (through this wrapper) — never duplicated.
/// </summary>
/// <typeparam name="TRequest">The wire DTO this command wraps.</typeparam>
/// <typeparam name="TResponse">The handler's payload type — the same type the wire operation returns.</typeparam>
public abstract record CommandRequest<TRequest, TResponse>(TRequest Request) : ICommandRequest<TResponse>
	where TRequest : notnull
	where TResponse : notnull;
