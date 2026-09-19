# CLAUDE.md

## Communication
Respond to the user in **French** during sessions. This file itself is in English for clarity/consistency.
Keep responses short. Do not add end-of-task summaries unless explicitly requested.

## Project
C# solution targeting **.NET 10**. All code, comments, console/UI output, commit messages and documentation are in **English**.

## Development workflow
Use unit tests (xUnit, `tests/VcvPatchBridge.Tests`) to drive development: write/update a test for new behavior or a bug before or alongside the fix, then run `dotnet test`. Don't consider a change done until the test suite passes.
