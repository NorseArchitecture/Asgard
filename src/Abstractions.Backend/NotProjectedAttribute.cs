namespace Norse.Abstractions.Backend;

/// <summary>
/// The one piece of promotion ceremony: excludes a declared entity scalar from the total-mirror law
/// (well-and-wire spec §4.2) — a concurrency token or bookkeeping column that deliberately does not
/// ride in the view document. Convention is the rule; this attribute marks the declared exception.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotProjectedAttribute : Attribute;
