# Norse.Abstractions.Worker

Norse worker abstractions — the server-side law for the system-of-record tier. Mutually invisible with `Norse.Abstractions.Web.Server`. Scaffolded, no source files yet. Planned surface: `IWorkerHostPlugin`, command and cached repository surfaces (`ICommandRepository<T>`, `ICachedRepository<T>`), and NServiceBus handler contract seams.

Part of the [Norse Architecture](https://github.com/NorseArchitecture) platform.
