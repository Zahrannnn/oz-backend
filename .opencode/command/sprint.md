---
description: Execute a sprint from BE_TASKS.md — delegate to sprint-builder agents, commit, verify.
agent: build
model: opencode-go/deepseek-v4-flash
---

Read `BE_TASKS.md` and find the next uncompleted sprint (Sprint $ARGUMENTS if specified, otherwise next sequential).

For that sprint:

1. **List all tasks** in that sprint section with their IDs, SP, and dependencies.

2. **Group tasks** by dependency chains:
   - Tasks with no dependencies on each other → run in parallel
   - Tasks that depend on others → run after their dependencies complete

3. **Delegate** each parallel group to `sprint-builder` agents via the Task tool:
   - Give each agent a detailed prompt with: task ID, acceptance criteria, relevant entity/controller context, and build verification
   - Wait for all agents in a group to complete before starting the next group

4. **Commit** each agent's work as a separate commit:
   - `git add` only the files that agent changed
   - Commit message: `feat: <task ID> <description>` or `fix: <description>`
   - Do NOT commit log files or secrets

5. **Verify** after all tasks:
   - `dotnet build` — must succeed with 0 errors
   - Start app: `dotnet run --project src/Api` with `ASPNETCORE_ENVIRONMENT=Development`
   - Smoke test key endpoints via `Invoke-RestMethod`
   - Report any failures

6. **Update docs** — add new endpoints to `docs/api/*.md` section files and update `docs/API_REFERENCE.md` index

7. **Report** — summarize: tasks completed, commits made, endpoints verified, any issues
