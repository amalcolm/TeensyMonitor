# AGENTS.md

## Working mode
- Default to read-only behavior even if sandbox allows writes.
- Do not modify files unless I say: "apply" (or strongly intimate I want changes made.)

## Edit policy
- First give a quick overview of the change needed, and formulate a patch, but please do not show patches in the chat output, unless extremely short (less than 8 lines total).
- Wait for my approval before applying any patch.
- If unclear, ask instead of editing.

## Commands
- Prefer read-only commands for exploration.
- Most files are below 1K in size so can be safely imported without trying to fetch parts of them.
- Ask before running commands that change files or run builds.
