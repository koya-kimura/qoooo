# Repository Rules

## Source of truth

Read the current architecture before changing code:

- `Docs/Architecture.md` (current repository entry point)
- `Docs/Architecture/Architecture.md` (intended nested entry point; may not exist yet)
- `Docs/Architecture/ParametersAndMidi.md`
- `Docs/Architecture/RenderingAndText.md`
- `Docs/Architecture/Runtime.md`
- `Docs/Architecture/Development.md`
- `Docs/Architecture/PlanStatus.md`

Implementation Plan documents are historical context. Do not treat them as the current specification or unfinished-task list. At task start, also read `Docs/CodexHandoff.md`.

## Build and verification

- Unity: `6000.3.12f1`
- Compile: `uloop compile --timeout-seconds 180`
- After implementation, normally complete: compile -> Play Mode verification -> git diff review.

## Working rules

- One task equals one focused change. Do not perform unrelated refactors.
- Do not change architecture unless explicitly requested.
- Do not independently change MIDI architecture, Parameter architecture, or Prefs schemas.
- Do not return to the legacy MIDI architecture.
- Do not casually add managers, services, interfaces, or abstractions.
- Read the owning implementation and its immediate call path before editing.
- Production time is short: prefer the smallest implementation that satisfies the task.
- Keep Scene changes minimal and explicit.
- Preserve Unity asset GUIDs; move or add the matching `.meta` file with every asset.

## Readability

The project owner must be able to follow the code. Prefer existing patterns and explicit data flow built from:

- `MonoBehaviour`
- typed Parameters
- private methods
- direct, visible control flow

Avoid complex abstraction when a local implementation is sufficient.

## Review handoff

An implementation task is incomplete until the final report includes:

1. `git status --short`
2. `git diff --stat` limited to the task's files
3. the complete C# diff
4. a list of Scene changes
5. the resulting data flow
6. compile and runtime verification results
