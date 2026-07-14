namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// The structured detail an <see cref="Outcome"/>/<see cref="Outcome{T}"/> carries on failure. Never
/// serialized directly — the mediator is server-only, in-process; the channel boundary (gRPC forwarder,
/// JSON controller) decomposes this into the transport's own failure signaling (RpcException/trailers,
/// ProblemDetails) rather than shipping this type over the wire.
/// </summary>
public sealed record Problem
{
	/// <summary>
	/// The error category.
	/// </summary>
	public required ErrorCategory Category { get; init; }

	/// <summary>
	/// Field-keyed validation or structured errors.
	/// </summary>
	public IReadOnlyDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}
