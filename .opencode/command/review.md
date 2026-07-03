---
description: Review code for over-engineering and unnecessary complexity. Lists what to cut.
agent: build
---

Review the codebase (or specified files) for over-engineering only. Correctness, security, and performance are out of scope.

Scan for:
- `delete:` Dead code, unused flexibility, speculative features
- `stdlib:` Hand-rolled thing the standard library ships
- `native:` Dependency doing what the platform already does
- `yagni:` Abstraction with one implementation, config nobody sets, layer with one caller
- `shrink:` Same logic, fewer lines

Focus areas:
- `src/Api/Controllers/` — duplicated patterns, over-abstracted base classes
- `src/Api/Services/` — interfaces with one implementation, unnecessary wrappers
- `src/Api/DTOs/` — unused DTOs, over-specified response shapes
- `src/Domain/Entities/` — speculative fields, unused navigation properties
- `src/Infrastructure/` — over-configured EF mappings, unnecessary repositories

Output format: one line per finding
`<file>:L<line>: <tag> <what>. <replacement>.`

End with: `net: -<N> lines possible.` or `Lean already. Ship.`
