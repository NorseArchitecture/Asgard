# Asgard

> The fortress of the Æsir, where law is declared and the cosmos answers to it.

<p align="center">
  <img src="https://github.com/user-attachments/assets/a5e0cfad-2b98-4f7b-a140-c7ca74e25bd3" alt="Asgard — the golden fortress of the Æsir, where the gods hold council and the laws that govern all nine realms are declared" title="Asgard — the fortress where law is declared and the cosmos answers to it" />
</p>

*Image credit: [@norsemythologyclips](https://www.instagram.com/norsemythologyclips/) — go follow them.*

Declared law for the Norse Architecture — **`Norse.Abstractions`**: the contracts and rules every realm must honor. No implementations live here, by design. Seven assemblies plus a source generator, split by dependency wall and consumer context:

| Assembly | Upstream | Purpose |
|---|---|---|
| [`Norse.Abstractions.Contracts`](src/Abstractions.Contracts) | `Norse.Primitives` | [`Outcome<T>`](src/Abstractions.Contracts/Outcome%7BT%7D.cs) with [`Problem`](src/Abstractions.Contracts/Problem.cs)/[`ErrorCategory`](src/Abstractions.Contracts/ErrorCategory.cs)/[`BoolResponse`](src/Abstractions.Contracts/BoolResponse.cs)/[`Unit`](src/Abstractions.Contracts/Unit.cs), and [`ErasureReceipt`](src/Abstractions.Contracts/ErasureReceipt.cs) |
| [`Norse.Abstractions.Components`](src/Abstractions.Components) | ASP.NET Core Components | Razor component base abstractions ([`AsyncComponentBase`](src/Abstractions.Components/AsyncComponentBase.cs), [`IAppShellLayout`](src/Abstractions.Components/Primitives/IAppShellLayout.cs), [`IDashboardWidget`](src/Abstractions.Components/Primitives/IDashboardWidget.cs)) — MAUI/WASM-safe, no server-side types |
| [`Norse.Abstractions.Backend`](src/Abstractions.Backend) | `Norse.Primitives`, `.Contracts` | The read contract ([`IReadRepository<TView>`](src/Abstractions.Backend/IReadRepository%7BTView%7D.cs), [`IViewBearer<TView>`](src/Abstractions.Backend/IViewBearer%7BTView%7D.cs)), the [serialization seam](src/Abstractions.Backend/Serialization), and the [key custody seam](src/Abstractions.Backend/Keys) |
| [`Norse.Abstractions.Web.Server`](src/Abstractions.Web.Server) | `.Backend`, `.Contracts`, FluentValidation | The [mediator law](src/Abstractions.Web.Server/Mediator) (`IRequest`/`ICommandRequest`/`IQueryRequest`, `ISender`, `IRequestHandler`, `IBehavior`, `CommandRequest`, `CommandRequestValidator`), the [gRPC facade](src/Abstractions.Web.Server/Facade), and [`IDeferredSignIn`](src/Abstractions.Web.Server/DeferredSignIn/IDeferredSignIn.cs) |
| [`Norse.Abstractions.Worker`](src/Abstractions.Worker) | `.Backend` | Declared and deliberately empty — worker-side contracts land with their first consumer |
| [`Norse.Abstractions.Migrations`](src/Abstractions.Migrations) | none | [`IMigrationContributor`](src/Abstractions.Migrations/IMigrationContributor.cs) and [`ISeedContributor`](src/Abstractions.Migrations/Seeding/ISeedContributor.cs) (EF-free) |
| [`Norse.Abstractions.Emit`](src/Abstractions.Emit) | none | The generator-authoring toolkit ([`CSharpEmit`](src/Abstractions.Emit/CSharpEmit.cs), [`Utf8NoBom`](src/Abstractions.Emit/Utf8NoBom.cs)) — netstandard2.0 so source generators across the platform can consume it |
| [`Abstractions.Web.Server.Generator`](gen/Abstractions.Web.Server.Generator) | `.Emit` | [`HandlerRegistrationGenerator`](gen/Abstractions.Web.Server.Generator/HandlerRegistrationGenerator.cs) — compile-time handler/dispatch/validator registration (`AddNorse{Realm}Handlers()`), bundled inside the `Norse.Abstractions.Web.Server` package |

`Worker` and `Web.Server` are mutually invisible — neither references the other.

## The law in force

- **[`Outcome<T>`](src/Abstractions.Contracts/Outcome%7BT%7D.cs)** — the platform's second discriminated union and the interior half of [the two unions](https://github.com/NorseArchitecture/Glitnir/blob/master/docs/the-two-unions.md): it faces operations inside the host and describes *an event* — "this operation ran; here is how it went" — where Svartálfheim's `Result<T>` faces the boundary and describes *data*. A hand-rolled `sealed class` union, `[MustConsume]`-decorated, deliberately starved API (`Ok`/`Err`/`Match` — no typed happy-path accessors, so the unhappy path cannot be politely glanced past). It is never serialized, stored, or compared; each transport edge translates it into its native tongue. Live today: Blazor components consume `Task<Outcome<T>>` from the gRPC service contracts and pattern-match the result.
- **The mediator law is server-only, deliberately** — the marker family lives in `Web.Server` so wire records shipped to WASM stay pure `[DataContract]` shapes with zero mediator coupling. Realms give a wire DTO server-side identity by deriving `CommandRequest<TRequest,TResponse>`, and the same client-side FluentValidation class runs again on the server through `CommandRequestValidator`.
- **The registration generator rides the contract package** — it discovers a realm's `IRequestHandler`/`IValidator` implementations at compile time and emits DI registration, replacing assembly scanning. It lives here (not in the infrastructure realm) because handler-bearing realms may legally depend only on declared law; duplicate handlers and requests without an `[Authorize]` policy are build errors (NORSE010/NORSE011).
- **`IMigrationContributor`** — the thinnest contract in the platform, live on NuGet, and the seed behind the migrations framework proven end to end across six realms (the full story is on [Bifröst's README](https://github.com/NorseArchitecture/Bifrost#readme)). No `Order`, no `DependsOn`: sequencing between contributors would couple bounded contexts.

Egress contracts (`Norse.Abstractions.Backend.Egress`) are staged, not yet executed. Every new surface follows the spec-first discipline: brainstorm → spec → plan in [Glitnir](https://github.com/NorseArchitecture/Glitnir)'s [docs/Asgard/](https://github.com/NorseArchitecture/Glitnir/tree/master/docs/Asgard), greenlit by the human, then code.

## Build and test

```shell
dotnet build Asgard.slnx   # warnings are errors — a single warning fails
dotnet test Asgard.slnx    # xUnit v3 + Shouldly on Microsoft.Testing.Platform
```

Requires the .NET 11 preview SDK pinned by `global.json`. The realm builds standalone — it is its own clone target, not only a Bifröst submodule.

## The naming law

Project folders and `.csproj` files are brand-free (`src/Abstractions.Contracts/Abstractions.Contracts.csproj`); the realm's root `Directory.Build.props` injects `AssemblyName` and `RootNamespace` as `Norse.$(MSBuildProjectName)`. Fork it, change `Norse` once, and every build output carries your brand — the `namespace Norse.*` declarations in code are yours to cull deliberately, with no filesystem change either way.

## The cosmos

Asgard is one realm of the [Norse Architecture](https://github.com/NorseArchitecture). The whole platform composes at [Bifröst](https://github.com/NorseArchitecture/Bifrost) — clone once, cross the bridge. Every design is tried in [Glitnir](https://github.com/NorseArchitecture/Glitnir), the design court, before law is declared here; this realm's specs and plans live in the court's [docs/Asgard/](https://github.com/NorseArchitecture/Glitnir/tree/master/docs/Asgard).

## Soundtrack: Twilight of the Thunder God
[![Soundtrack: Twilight of the Thunder God](https://img.youtube.com/vi/JFYVcz7h3o0/maxresdefault.jpg)](https://www.youtube.com/watch?v=JFYVcz7h3o0)
