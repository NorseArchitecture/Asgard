namespace Norse.Abstractions.Web.Server.Mediator;

/// <summary>
/// Declares a custom behavior for the generated in-process gateway chain. Decorates the service
/// <b>implementation</b> class or a specific method on it — never the service interface, which lives
/// in a <c>.Components</c> project shipped to WASM; a <see cref="Type"/> argument there would force
/// that assembly to reference the behavior's (server-side) implementation assembly (spec §2.5 defect 2).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class BehaviorAttribute(Type behaviorType, Type? after = null) : Attribute
{
	/// <summary>The <see cref="IBehavior{TRequest,TResponse}"/> implementation to insert.</summary>
	public Type BehaviorType { get; } = behaviorType;

	/// <summary>The standard behavior this one runs after in the chain; <see langword="null"/> inserts immediately after Validation, before the handler.</summary>
	public Type? After { get; } = after;
}
