# Norse.Abstractions.Web.Server

Norse web-server abstractions — the server-side law for the web tier. Mutually invisible with `Norse.Abstractions.Worker`. Live surface: the `IDeferredSignIn` contract and the mediator law (`ICommandRequest<TResponse>`, `IRequestHandler<TRequest, TResponse>`, `IBehavior<TRequest, TResponse>`, `BehaviorAttribute`) built over `Norse.Abstractions.Contracts`' `Outcome<T>`. `IWebHostPlugin` and the document repository surface (`IDocumentRepository<T>`) are scaffolded, not yet implemented.

Part of the [Norse Architecture](https://github.com/NorseArchitecture) platform.
