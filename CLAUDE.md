# CLAUDE.md — Asgard (`Norse.Abstractions`)

## 0. Wrong Root — Halt

If you are reading this because **Asgard itself is the Claude Code session root** — someone ran `claude` from inside this directory instead of `../Bifrost` — stop here. Do not read further, do not propose changes, do not run anything.

Tell the user: every Norse Architecture session starts from **Bifrost**. Org-wide settings (the `superpowers` plugin, permission rules) only apply when Bifrost is the actual session root — Claude Code never merges a submodule's own `.claude/settings.json` into a parent-launched session. Exit, `cd ../Bifrost`, and run `claude` there instead.

This repo's own `.claude/settings.json` carries a `SessionStart` hook that should already have blocked this session before this file was ever read. If you're reading this anyway, hooks were bypassed, disabled, or failed — halt regardless; this rule does not depend on the hook to hold.

---

> **Do not commit, push, or rewrite git history.** Stage edits (`git add`), show the diff, and stop — the human reviews and commits.

> **Use US English spelling** in code, identifiers, comments, docs, and commit/PR copy.

## 1. What This Repository Is

Asgard is **declared law** — `Norse.Abstractions`: contracts and the rules every realm must honor. No implementations live here, by design. Six assemblies, split by dependency wall and consumer context — see `../Glitnir/docs/Asgard/specs/2026-06-25-asgard-project-structure-design.md` for the full assembly set, dependency graph, and rationale.

The dependency graph is peer-flat except for one assembly: `Norse.Abstractions.Backend` depends on `Norse.Abstractions.Contracts` and `Norse.Primitives` (Svartalfheim — forged below the domain, per the platform convention). The five remaining assemblies carry no upstream dependencies. "Asgard rides on nothing" was the claim before specs converged; the settled design shows `Norse.Abstractions.Backend` is the exception.

This repo is scaffolded — six source projects and six test projects wired into `Asgard.slnx`. The first implementation is the egress contracts slice (plan: `../Glitnir/docs/Asgard/plans/2026-06-19-asgard-egress-contracts.md`, Tasks 2–6 with the amendment applied — egress types land in `Norse.Abstractions.Backend.Egress`). Every subsequent plan for this realm follows the same discipline: brainstorm → spec → plan in `../Glitnir/docs/Asgard/`, greenlit by the human, then code. Each plan's REQUIRED SUB-SKILL line names `superpowers:subagent-driven-development` as the default (not a recommendation among equals — `executing-plans` is the narrow fallback for separate-session review checkpoints) paired with `superpowers:test-driven-development` — implementation here is subagent-orchestrated and test-driven, never one without the other (`../Glitnir/CLAUDE.md` §2.8).

See `../Bifrost/CLAUDE.md` (§2 The Naming Model) and `../Glitnir/CLAUDE.md` (§1 Bounded Context Map) for the full realm table and how Asgard fits the rest of the cosmos.
