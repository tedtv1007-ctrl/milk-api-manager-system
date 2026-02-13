# Memory

## Perpetual Motion Mode (永動機模式)
- **Target**: Collaborator agent `skihelp`.
- **Logic**: Periodically check for open issues assigned to `skihelp` across all active repositories (`milk-api-manager-system`, `enterprise-ai-knowledge-integration`, `milk-ai-mcp-insuretech`, `enterprise-k8s-architecture-research`).
- **Trigger**: If `skihelp` has fewer than 2 active tasks, proactively create or assign new tasks from the project roadmap or backlog.
- **Goal**: Ensure `skihelp` never stays idle.

## Silent Replies
When you have nothing to say, respond with ONLY: NO_REPLY
⚠️ Rules:
- It must be your ENTIRE message — nothing else
- Never append it to an actual response (never include "NO_REPLY" in real replies)
- Never wrap it in markdown or code blocks
❌ Wrong: "Here's help... NO_REPLY"
❌ Wrong: "NO_REPLY"
✅ Right: NO_REPLY

## Heartbeats
Heartbeat prompt: Read HEARTBEAT.md if it exists (workspace context). Follow it strictly. Do not infer or repeat old tasks from prior chats. If nothing needs attention, reply HEARTBEAT_OK.
If you receive a heartbeat poll (a user message matching the heartbeat prompt above), and there is nothing that needs attention, reply exactly:
HEARTBEAT_OK
OpenClaw treats a leading/trailing "HEARTBEAT_OK" as a heartbeat ack (and may discard it).
If something needs attention, do NOT include "HEARTBEAT_OK"; reply with the alert text instead.
