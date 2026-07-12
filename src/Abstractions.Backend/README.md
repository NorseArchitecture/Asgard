# Norse.Abstractions.Backend

Norse server-side shared contracts, visible to both Worker and Web.Server. Egress contracts live under the `Norse.Abstractions.Backend.Egress` namespace: `HttpResult<T>`, `EgressError`, `FailureKind`, `ResponseDisposition`, `EgressClassifier`, `IResponseParser<T>`, `IHttpEgress`. Additional server-side shared concerns land here as they emerge; a concern graduates to its own assembly only if a hard wall requires it.

Part of the [Norse Architecture](https://github.com/NorseArchitecture) platform.
