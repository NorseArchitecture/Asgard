namespace Norse.Abstractions.Contracts.Generator;

sealed record GatewayMethodModel(string Name, string RequestTypeName, string? ResponseTypeName, string PolicyName);
