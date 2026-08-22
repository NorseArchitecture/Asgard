namespace Norse.Abstractions.Contracts;

/// <summary>
///     The transport shape one <see cref="ErrorCategory" /> answers with, declared once and projected by
///     every edge. Carries plain integers deliberately: this assembly is client-safe and ships into WASM
///     and MAUI, so it may reference neither <c>Microsoft.AspNetCore.Http.StatusCodes</c> nor
///     <c>Grpc.Core.StatusCode</c>. Each edge casts to its own enum at the point of use.
/// </summary>
/// <param name="HttpStatus">The HTTP status code this category folds to at a text-channel edge.</param>
/// <param name="GrpcStatus">The <c>Grpc.Core.StatusCode</c> integer value this category folds to.</param>
/// <param name="BodyPermitted">
///     Whether a response for this category may carry a body at all. <see langword="false" /> for the
///     silent categories: the platform never explains a failed authentication attempt, so there is no
///     branch anywhere that may attach one.
/// </param>
public readonly record struct TransportDisposition(int HttpStatus, int GrpcStatus, bool BodyPermitted);
