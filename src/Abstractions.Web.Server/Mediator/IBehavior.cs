using System.Diagnostics.CodeAnalysis;
using Norse.Abstractions.Contracts;

namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>Continues to the next behavior (or the handler) in a generated in-process chain.</summary>
[SuppressMessage("Design", "CA1711:Identifiers should not have incorrect suffix", Justification = "Delegate naming is idiomatic; 'Delegate' suffix is appropriate for delegate types.")]
public delegate ValueTask<Outcome<TResponse>> BehaviorDelegate<TResponse>() where TResponse : notnull;

/// <summary>
/// One link in the generated in-process gateway's behavior chain (spec §2.5). Standard behaviors
/// (Telemetry, ExceptionTranslation, Authorization, Validation) live in Midgard; a product realm's
/// custom behavior implements this same contract. One family only — void-success operations use
/// <c>TResponse = Unit</c> (spelled <c>Outcome</c> via the platform-wide alias), never a second,
/// non-generic <c>IBehavior&lt;TRequest&gt;</c> form. That fork was drafted and reverted before
/// Asgard's ship gate specifically to avoid forcing every future custom behavior — including every
/// {Company}.{Context} product realm's own — to implement both shapes forever.
/// </summary>
public interface IBehavior<TRequest, TResponse> where TResponse : notnull
{
	/// <summary>Runs this behavior, calling <paramref name="next"/> to continue the chain.</summary>
	[SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Behavior chain signature places CancellationToken before the delegate for clarity in the generated code.")]
	[SuppressMessage("Naming", "CA1716:Identifiers should not conflict with keywords", Justification = "Parameter name 'next' is intentional for clarity in behavior chaining.")]
	ValueTask<Outcome<TResponse>> Handle(TRequest request, CancellationToken cancellationToken, BehaviorDelegate<TResponse> next);
}
