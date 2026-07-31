namespace Norse.Abstractions.Backend;

/// <summary>
/// The well law: an entity exposing a denormalized read view implements this beside its Id-bearing
/// base, and that is the entire per-well contract for read access — a well that models its entities
/// correctly gets its repositories by existing (well-and-wire spec §3.1). The view is a total mirror
/// of the entity's declared non-FK scalars (spec §4.2); Midgard's <c>AddWell</c> validates the
/// mirror at startup and throws on any missing pair.
/// </summary>
/// <typeparam name="TView">The owned-JSON read-model document type.</typeparam>
public interface IViewBearer<TView>
{
	/// <summary>The JSON-mapped document column.</summary>
	TView View { get; }
}
