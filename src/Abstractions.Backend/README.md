# Norse.Abstractions.Backend

Norse server-side shared contracts, visible to both Worker and Web.Server. Scaffolded, no source files yet. The egress contracts slice — `Norse.Abstractions.Backend.Egress`: `HttpResult<T>`, `EgressError`, `FailureKind`, `ResponseDisposition`, `EgressClassifier`, `IResponseParser<T>`, `IHttpEgress` — is staged in Glitnir but not executed; the transport-neutral-gateway work landed first. Additional server-side shared concerns land here as they emerge; a concern graduates to its own assembly only if a hard wall requires it.

Part of the [Norse Architecture](https://github.com/NorseArchitecture) platform.
