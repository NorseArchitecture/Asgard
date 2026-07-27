# Asgard

> The fortress of the Æsir, where law is declared and the cosmos answers to it.

![Asgard — the golden fortress of the Æsir, where the gods hold council and the laws that govern all nine realms are declared](https://github.com/user-attachments/assets/a5e0cfad-2b98-4f7b-a140-c7ca74e25bd3 "Asgard — the fortress where law is declared and the cosmos answers to it")

*Image credit: [@norsemythologyclips](https://www.instagram.com/norsemythologyclips/) — go follow them.*

Declared law for the Norse Architecture — **`Norse.Abstractions`**: the contracts and rules every realm must honor. No implementations live here, by design. Six assemblies, split by dependency wall and consumer context:

| Assembly | Upstream Dependencies | Purpose |
|---|---|---|
| `Norse.Abstractions.Contracts` | none | `Outcome<T>` (+ `Problem`/`ErrorCategory`/`BoolResponse`/`Unit`) |
| `Norse.Abstractions.Components` | none | Razor component base abstractions (MAUI/WASM-safe — no server-side infrastructure) |
| `Norse.Abstractions.Backend` | `Norse.Primitives`, `Norse.Abstractions.Contracts` | Shared server-side contracts (egress contracts under `.Egress` namespace) |
| `Norse.Abstractions.Worker` | `Norse.Abstractions.Backend` (transitive) | `IWorkerHostPlugin`, `ICommandRepository<T>`, `ICachedRepository<T>`, NServiceBus seams |
| `Norse.Abstractions.Web.Server` | `Norse.Abstractions.Backend` (transitive) | `IWebHostPlugin`, `IDocumentRepository<T>`, mediator law (`IRequest`/`ICommandRequest`/`IQueryRequest` marker family, `ISender`, `IRequestHandler`, `IBehavior`, `IPrincipalAccessor`) |
| `Norse.Abstractions.Migrations` | none | `IMigrationContributor` (EF-free) |

Worker and Web.Server are mutually invisible — neither references the other.

## Status

Scaffolded — six source projects and six test projects, wired into `Asgard.slnx`. **`Norse.Abstractions.Migrations` shipped first** — `IMigrationContributor` is live on NuGet, the seed contract behind the platform-wide migrations framework proven end to end across six realms (the full story is on [Bifröst's README](https://github.com/NorseArchitecture/Bifrost#readme)). **`Norse.Abstractions.Web.Server` also carries a live `IDeferredSignIn` contract** (Midgard implements it) and the mediator law surface (`IRequestHandler`, `ICommandRequest`, `IBehavior`). **`Norse.Abstractions.Contracts` shipped `Outcome<T>`** — the platform's second discriminated union, Asgard's counterpart to Svartálfheim's `Result<T>`. The code-generated gateway layer that previously lived here (`GatewayGenerator`, `[GenerateGateway]`, `Contract`/`InProcessHost`/`WireHost` emission) is retired in favor of a hand-rolled mediator pipeline — the marker family (`IRequest`/`ICommandRequest`/`IQueryRequest`) plus `IRequestHandler`/`ISender`/`ISenderDispatch`/`IBehavior`/`IPrincipalAccessor` all live in Web.Server, deliberately server-only, with the handler-registration generator (`gen/Abstractions.Web.Server.Generator`) live ([design](https://github.com/NorseArchitecture/Glitnir/blob/master/docs/Platform/specs/2026-07-27-mediator-pipeline-retires-gateway-design.md)). Egress contracts (`Norse.Abstractions.Backend.Egress`) remain staged, not yet executed. Design for each subsequent type surface follows the spec-first discipline: brainstorm → spec → plan in [Glitnir](https://github.com/NorseArchitecture/Glitnir)'s `docs/Asgard/`, greenlit by the human, then code.

## The cosmos

Asgard is one realm of the [Norse Architecture](https://github.com/NorseArchitecture). The whole platform composes at [Bifröst](https://github.com/NorseArchitecture/Bifrost) — clone once, cross the bridge, and every session starts there so decisions get brainstormed across the entire landscape, not in isolation. Every design is tried in [Glitnir](https://github.com/NorseArchitecture/Glitnir), the design court, before code is forged here; this realm's specs and plans will live in the court's [docs/Asgard/](https://github.com/NorseArchitecture/Glitnir/tree/master/docs/Asgard) once they converge.

## Soundtrack: Twilight of the Thunder God
[![Soundtrack: Twilight of the Thunder God](https://img.youtube.com/vi/JFYVcz7h3o0/maxresdefault.jpg)](https://www.youtube.com/watch?v=JFYVcz7h3o0)
