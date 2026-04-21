---
description: Investigate and fix browser-extension-specific issues in src/BlazorDevTools.Extension
agent: extension-agent
---

/ext-fix <goal>

Use this command only for extension-specific debugging or implementation work in `src/BlazorDevTools.Extension`.

Typical goals:

- inspect mode broken
- tree/details rendering issue
- snapshot relay issue
- panel selection/search issue
- manifest/content script/devtools wiring issue

Instructions:

Treat `$ARGUMENTS` as the extension problem to investigate and fix.

Stay focused on:

- `src/BlazorDevTools.Extension/src/panel.ts`
- `src/BlazorDevTools.Extension/src/content.ts`
- `src/BlazorDevTools.Extension/src/devtools.ts`
- `src/BlazorDevTools.Extension/static/*`

Only reach into runtime/protocol code if the extension issue clearly depends on it.

Validate with:

- `npm ci` in `src/BlazorDevTools.Extension` if needed
- `npm run build` in `src/BlazorDevTools.Extension`

If runtime interaction is involved, summarize exactly whether the bug was:

- extension-only
- message-chain related
- runtime/protocol dependent

Return:

- root cause
- files changed
- validation run
- any remaining browser-only manual verification needed
