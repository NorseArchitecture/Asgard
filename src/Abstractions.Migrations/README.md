# Norse.Abstractions.Migrations

Norse migration contract: the `IMigrationContributor` interface (EF-free) — the single law governing migration contribution across all contexts. Not referenced by Worker or Web.Server; isolation enforced by the absence of a project reference. Also carries `ISeedContributor`, the second-phase seeding contract.

Part of the [Norse Architecture](https://github.com/NorseArchitecture) platform.
