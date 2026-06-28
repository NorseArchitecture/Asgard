# Asgard

> The fortress of the Æsir, where law is declared and the cosmos answers to it.

![Asgard — the golden fortress of the Æsir, where the gods hold council and the laws that govern all nine realms are declared](https://github.com/user-attachments/assets/a5e0cfad-2b98-4f7b-a140-c7ca74e25bd3 "Asgard — the fortress where law is declared and the cosmos answers to it")

*Image credit: [@norsemythologyclips](https://www.instagram.com/norsemythologyclips/) — go follow them.*

Declared law for the Norse Architecture — **`Norse.Abstractions`**: the contracts and rules every realm must honor. No implementations live here, by design. Six assemblies, split by dependency wall and consumer context:

| Assembly | Upstream Dependencies | Purpose |
|---|---|---|
| `Norse.Abstractions.Contracts` | none | `NorsePrincipal`, `Population`, published event interfaces, `IAccountApi` |
| `Norse.Abstractions.Components` | none | Razor component base abstractions (MAUI/WASM-safe — no server-side infrastructure) |
| `Norse.Abstractions.Backend` | `Norse.Primitives`, `Norse.Abstractions.Contracts` | Shared server-side contracts (egress contracts under `.Egress` namespace) |
| `Norse.Abstractions.Worker` | `Norse.Abstractions.Backend` (transitive) | `IWorkerHostPlugin`, `ICommandRepository<T>`, `ICachedRepository<T>`, NServiceBus seams |
| `Norse.Abstractions.Web.Server` | `Norse.Abstractions.Backend` (transitive) | `IWebHostPlugin`, `IDocumentRepository<T>`, mediator law |
| `Norse.Abstractions.Migrations` | none | `IMigrationContributor` (EF-free) |

Worker and Web.Server are mutually invisible — neither references the other.

## Status

Scaffolded — six source projects and six test projects, wired into `Asgard.slnx`. First implementation in progress: the egress contracts slice (`Norse.Abstractions.Backend.Egress`). Design for each subsequent type surface follows the spec-first discipline: brainstorm → spec → plan in [Glitnir](https://github.com/NorseArchitecture/Glitnir)'s `docs/Asgard/`, greenlit by the human, then code.

## The cosmos

Asgard is one realm of the [Norse Architecture](https://github.com/NorseArchitecture). The whole platform composes at [Bifröst](https://github.com/NorseArchitecture/Bifrost) — clone once, cross the bridge, and every session starts there so decisions get brainstormed across the entire landscape, not in isolation. Every design is tried in [Glitnir](https://github.com/NorseArchitecture/Glitnir), the design court, before code is forged here; this realm's specs and plans will live in the court's `docs/Asgard/` once they converge.
