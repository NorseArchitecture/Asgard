# CLAUDE.md — Asgard (`Norse.Abstractions`)

## 0. Wrong Root — Halt

Session root must be **Bifröst**, never this repo. Org-wide settings (`superpowers`, permission rules) only apply from the actual root, and Claude Code never merges a submodule's own `.claude/settings.json` into a parent-launched session. If `claude` was launched inside Asgard: stop — don't read further, don't propose changes, don't run anything — tell the user to `cd ../Bifrost` and start there. (A `SessionStart` hook should block this before you ever see this file; if you're reading this anyway, halt regardless.)

> **Never commit, push, or rewrite git history** — stage (`git add`), show the diff, stop; the human commits. This holds even when a skill's flow includes a commit step. **US English spelling** everywhere — code, comments, docs, commits.

## What This Realm Is

Declared law — `Norse.Abstractions`: contracts and the rules every realm must honor. **No implementations live here, by design.** Seven source assemblies plus one source generator, split by dependency wall and consumer context:

| Assembly | Depends on | Carries |
|---|---|---|
| `Abstractions.Contracts` | `Norse.Primitives` | `Outcome<T>` (`Success<T>`/`Failed(Problem)`) + `Problem`/`ErrorCategory`/`BoolResponse`/`Unit`, `ErasureReceipt` |
| `Abstractions.Components` | `Abstractions.Contracts`, ASP.NET Core Components, Blazilla (pulls in FluentValidation 12.1.1 transitively — by design, see Architecture Facts) | `AsyncComponentBase`, `IAppShellLayout`, `IDashboardWidget`, and the outcome-form seam hoisted from Heimdall 2026-08-09: `OutcomeFormComponentBase` (stamped-request `EditContextFor` mechanic), `FormValidator` (attaches Blazilla's FluentValidation pass, stamps `FormProperties.ValidatorAttached`) + `ServerValidation/` (`ServerErrorCoordinator`, `EditContextServerErrorsExtensions`, `CategoryDisplay`) — MAUI/WASM-safe, no server types |
| `Abstractions.Backend` | `Abstractions.Contracts` | Read contract (`IReadRepository<TView>`, `IViewBearer<TView>`, `NotProjectedAttribute`); serialization seam (`Serialization/`); key seam (`Keys/`) |
| `Abstractions.Web.Server` | `Abstractions.Backend`, FluentValidation | Mediator law (`Mediator/`), gRPC facade (`Facade/`), `IDeferredSignIn` |
| `Abstractions.Worker` | `Abstractions.Backend` | **Empty — declared, no contracts yet.** Types land with their first consumer; docs listing `IWorkerHostPlugin`/repo contracts here were aspirational and are retired |
| `Abstractions.Migrations` | none | `IMigrationContributor`, `Seeding/ISeedContributor` |
| `Abstractions.Emit` | none (netstandard2.0) | `CSharpEmit.AppendCSharp`, `Utf8NoBom` — the generator-authoring toolkit, consumed by generators platform-wide |
| `gen/Abstractions.Web.Server.Generator` | Emit | `HandlerRegistrationGenerator` — ships **inside** the `Norse.Abstractions.Web.Server` package as a bundled analyzer |

`Worker` and `Web.Server` are mutually invisible — neither references the other. Eight test projects under `tests/` — one per source assembly plus one for the generator; the mediator pipeline law is tested inside `Abstractions.Web.Server.Tests` (`Mediator/` plus `SenderDispatchTests`), not in a dedicated project. Every test project contains at least one test, deliberately.

**Spec index** — under `../Glitnir/docs/` (execution plans sit beside specs under `plans/`):

| Subject | Document |
|---|---|
| Assembly set, dependency graph, rationale | `Asgard/specs/2026-06-25-asgard-project-structure-design.md` |
| Mediator pipeline (retires the gateway layer) | `Platform/specs/2026-07-27-mediator-pipeline-retires-gateway-design.md` |
| Generator toolkit + raw-string house style | `Asgard/specs/2026-07-25-generator-authoring-toolkit-and-raw-string-house-style-design.md` |
| Read contract / well-and-wire ruling | `Platform/specs/2026-07-30-well-and-wire-reference-data-slice-design.md` |
| Serialization seam | `Platform/specs/2026-08-03-serialization-seam-design.md` |
| Key seam + `ErasureReceipt` (PII) | `Platform/specs/2026-08-03-pii-primitives-identity-erasure-seam-design.md` |
| Migrations framework rollout | `Platform/plans/2026-06-28-migrations-framework-identity-schema.md` |
| `IDeferredSignIn` placement | `Heimdall/plans/2026-07-14-deferred-signin-fix.md` |
| Egress contracts (staged, not executed) | `Asgard/plans/2026-06-19-asgard-egress-contracts.md` |
| Form validation hoist (`FormValidator`, gated `SubmitAsync`) | `Asgard/plans/2026-08-10-form-validation-hoist.md` |
| The two unions | `the-two-unions.md` |

## Build & Test

- `dotnet build Asgard.slnx` — warnings are errors; a single warning fails.
- `dotnet test Asgard.slnx` — xUnit v3 + Shouldly on Microsoft.Testing.Platform. **VSTest `--filter` does NOT work** — use `dotnet test tests/Abstractions.Contracts.Tests -- --filter-class "*.OutcomeTests"`.
- **NEVER `dotnet test` a test project containing zero tests** — xUnit v3 fails the run (every current test project has tests; keep it that way when adding projects).
- SDK pinned by `global.json`: `11.0.100-` prerelease, rollForward latestFeature.

## Architecture Facts (decided — do not re-litigate)

- **`Outcome<T>` is the platform's second discriminated union** — a hand-rolled `sealed class` `[Union]` provider pattern (the C# 15 `union` keyword compiles exclusively to a record struct, which cannot cross gRPC client machinery and carries wrong equality for an event), `[MustConsume]`-decorated, API deliberately starved (`Ok`/`Err`/`Match` + blessed escapes; no typed happy-path accessors). Never serialized, never stored, never compared — translation happens at the transport edge (`OutcomeServerInterceptor` in Midgard for gRPC). Opposite polarity to Svartálfheim's `Result<T>`; full doctrine: `the-two-unions.md` (index above). Live today: components consume `Task<Outcome<T>>` from the gRPC service contracts and pattern-match the result.
- **The mediator marker family is server-only law, deliberately** — `IRequest<T>`/`ICommandRequest<T>`/`IQueryRequest<T>` live in `Abstractions.Web.Server` (ruled 2026-07-27, wire-purity amendment) so a WASM-shipped assembly referencing only `Abstractions.Contracts` cannot even name them. Wire `[DataContract]` records stay pure; `CommandRequest<TRequest,TResponse>` is the wrapper a realm derives to give a wire DTO mediator identity server-side, and `CommandRequestValidator<TCommand,TRequest,TResponse>` reruns the client-side FluentValidation class against the wrapped request on the server.
- **The handler-registration generator lives here because the Law of the Realms leaves it nowhere else** — it keys on this assembly's own `IRequestHandler<,>`/`IValidator<>` and ships inside the `Norse.Abstractions.Web.Server` package, so every handler-bearing realm (Himinbjörg's `Identity.Web.Server`, Mímir's `Reference.Web.Server`, every future context) gets `AddNorse{Realm}Handlers()` from the one dependency it is legally allowed to take — a Midgard home would make every leaf a NORSE071 conviction. Emits handler + dispatch-map + validator registrations; strikes: NORSE010 (duplicate handler for a request), NORSE011 (request missing `[Authorize(Policy = ...)]` — `AuthNPolicies.Public` included, no unmarked requests).
- **Generator emitters never call `AppendLine` directly** — always `sb.AppendCSharp(...)` (`Abstractions.Emit`, `[StringSyntax("C#")]`), collapsing sequential appends into one raw string literal so generated shape reads as a block. `Abstractions.Emit` is netstandard2.0 precisely so generators can consume it.
- **`IMigrationContributor` is deliberately the thinnest contract in the platform** — no `Order`, no `DependsOn`: sequencing between migration contributors would be coupling between bounded contexts, which platform law forbids outright.
- **`IReadRepository<TView>` is the read contract, implemented exactly once** — by Midgard's generic repository over `IViewBearer<TView>` entities (2026-07-30 ruling, superseding the former four-contract family — `IDocumentRepository<T>`/`ICommandRepository<T>`/`ICachedRepository<T>` are gone, not pending). The write-side contract lands at the same address when designed.
- **`SubjectKeyResult` is deliberately neither `Result<T>` nor `Outcome<T>`** — a seam-local closed three-state union for the key custody seam (`Keys/`: `ISubjectKeyStore`, `ILookupKeyRing`, `SubjectCryptoScope` ambient write-subject, `KeyDestroyedException`/`KeyMissingException`). Do not propose unifying it with either platform union.
- **`IDeferredSignIn` is declared here so Midgard can implement it** without Himinbjörg taking a Midgard dependency for a hosting concern.
- **FluentValidation is a platform-wide bet, not a dependency to minimize** (ruled 2026-08-10). `Abstractions.Components` takes Blazilla, which pulls FluentValidation 12.1.1 into every consumer with no opt-out — including consumers that never render a form. That is the intent: write the request, response, validator, and handler once, and the one validator class runs client-side in the form and server-side via `CommandRequestValidator<,,>` in the mediator pipeline. The platform does not own a validation framework and does not own its Blazor integration. Do not propose an opt-out seam, a `Forms`-suffixed split package, or a hand-rolled adapter to shed the transitive reference.
- **The gateway layer is deleted, not dormant** — `GatewayGenerator`, `[GenerateGateway]`, and all emission modes are gone (2026-07-27); the empty `gen/Abstractions.Contracts.Generator` folders on disk are untracked residue, not a project.

## Process

Egress contracts (`Abstractions.Backend.Egress`) are staged, not executed — plan in the index above. Every new surface is spec-first: brainstorm → spec → plan in `../Glitnir/docs/Asgard/`, human greenlight at each transition. Implementation is subagent-orchestrated and test-driven, always: every plan's REQUIRED SUB-SKILL line names `superpowers:subagent-driven-development` (the default; `superpowers:executing-plans` is the narrow separate-session fallback) paired with `superpowers:test-driven-development`. Full rule: `../Glitnir/CLAUDE.md` §2.8.

See `../Bifrost/CLAUDE.md` (§2 The Naming Model) and `../Glitnir/CLAUDE.md` (§3 Bounded Context Map) for the full realm table and how Asgard fits the cosmos.
