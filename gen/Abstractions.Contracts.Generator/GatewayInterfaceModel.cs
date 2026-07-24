using System.Collections.Immutable;

namespace Norse.Abstractions.Contracts.Generator;

sealed record GatewayInterfaceModel(string Namespace, string ServiceInterfaceName, string ContextName, ImmutableArray<GatewayMethodModel> Methods);
