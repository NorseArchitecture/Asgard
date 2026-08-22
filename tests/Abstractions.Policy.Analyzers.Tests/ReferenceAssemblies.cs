using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Norse.Abstractions.Policy.Analyzers.Tests;

static class ReferenceAssemblies
{
	public static readonly MetadataReference[] Net110 =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.ComponentModel").Location)
	];
}
