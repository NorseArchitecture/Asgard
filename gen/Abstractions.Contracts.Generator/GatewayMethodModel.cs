namespace Norse.Abstractions.Gateway.Generator;

sealed record GatewayMethodModel(string Name, string RequestTypeName, string? ResponseTypeName, string PolicyName);
