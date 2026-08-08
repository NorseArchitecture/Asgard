namespace Norse.Abstractions.Backend.Serialization;

/// <summary>
///     The property-naming convention an <see cref="ISerializer" /> applies when writing and reading
///     payloads. Conventions are format-agnostic — a JSON serializer maps them to its naming policies;
///     any other format maps them however that format spells casing.
/// </summary>
public enum NamingStrategy
{
	/// <summary>Sentinel CLR default — never a valid strategy; a caller always names its convention.</summary>
	Unspecified = 0,

	/// <summary>Property names are written in camelCase (e.g. <c>myProperty</c>).</summary>
	CamelCase = 1,

	/// <summary>Property names are written in PascalCase (e.g. <c>MyProperty</c>).</summary>
	PascalCase = 2,

	/// <summary>Property names are written in snake_case (e.g. <c>my_property</c>).</summary>
	SnakeCase = 3,

	/// <summary>Property names are written in kebab-case (e.g. <c>my-property</c>).</summary>
	KebabCase = 4
}
