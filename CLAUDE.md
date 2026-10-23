@AGENTS.md

## Claude Code specifics

- **Governance audit:** after each task, delegate the review of the task diff to the `governance-auditor` subagent. It runs in its own context, so its reading does not consume the main session's context.
- **One task per session:** run `/clear` between tasks; the task payload is rebuilt from `.sdap/` and `docs/ai/`, not from conversation history.
- **Model by complexity:** most capable model for design/inception and tricky concurrency or security tasks; a mid-tier model for routine implementation tasks and for the auditor.
- **Library documentation:** fetch it on demand (Context7 MCP when available) instead of pasting docs into the prompt.
- **Token log:** after each task, record the session usage reported by `/cost` in `docs/ai/token-log.md`.
