namespace Norse.Abstractions.Contracts;

/// <summary>
/// Opts a <c>[ServiceContract]</c> interface into gateway generation (spec §2.2, §2.4). Every method
/// on a decorated interface must carry <c>[Authorize(Policy = ...)]</c> — enforced by the generator
/// as a build error (spec decided law item 4).
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class GenerateGatewayAttribute : Attribute;
