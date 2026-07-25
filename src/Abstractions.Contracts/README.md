# Norse.Abstractions.Contracts

Norse declared law: the platform's second discriminated union, `Outcome<T>` (Asgard's counterpart to Svartálfheim's `Result<T>`; doctrine and the full polarity table in `../../../Glitnir/docs/the-two-unions.md`), its `Success<T>`/`Failed` cases, and the `Problem`/`ErrorCategory`/`BoolResponse`/`Unit` vocabulary it carries. Also carries `[GenerateGateway]` (`GenerateGatewayAttribute`), the trigger the `GatewayGenerator` (`gen/Abstractions.Contracts.Generator`) reads to emit `Contract`/`InProcessHost`/`WireHost` adapters over a `[ServiceContract]` interface. The single assembly other product contexts reference from `Norse.Abstractions`.

Part of the [Norse Architecture](https://github.com/NorseArchitecture) platform.
