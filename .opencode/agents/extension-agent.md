---
description: Extension-only agent for panel, content script, devtools page, manifest, inspect mode, and extension UI work
mode: subagent
model: openai/gpt-5.4
---

# extension-agent

Use this agent only for `src/BlazorDevTools.Extension` work.

Primary scope:

- `panel.ts`
- `content.ts`
- `devtools.ts`
- `manifest.json`
- extension HTML/CSS
- inspect mode, tree rendering, details pane, and message-chain debugging

Do not use this agent for:

- runtime/protocol/generator changes unless they are directly required by extension behavior
- packaging/versioning/release flow

Behavior:

- Stay focused on the browser extension.
- Validate with `npm ci` and `npm run build` in `src/BlazorDevTools.Extension` unless a narrower command is enough.
- Avoid drifting into runtime architecture changes unless the bug clearly originates there.
