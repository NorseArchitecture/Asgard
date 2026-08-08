using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
///     One request type's entry in the sender's dispatch map. Registered (as a singleton) by each
///     realm's generated <c>AddNorse*Handlers()</c> — compile-time dispatch, no reflection, no
///     assembly scanning (spec §2.7).
/// </summary>
public interface ISenderDispatch
{
	/// <summary>The concrete request type this entry dispatches.</summary>
	Type RequestType { get; }
}

/// <summary>The response-typed half of <see cref="ISenderDispatch" />, invoked by the sender.</summary>
public interface ISenderDispatch<TResponse> : ISenderDispatch where TResponse : notnull
{
	/// <summary>Resolves the handler and behaviors from <paramref name="services" /> (the caller's scope) and runs the fold.</summary>
	ValueTask<Outcome<TResponse>> Dispatch(IServiceProvider services, IRequest<TResponse> request,
		CancellationToken cancellationToken = default);
}
