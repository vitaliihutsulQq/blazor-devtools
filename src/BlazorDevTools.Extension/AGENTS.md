# AGENTS.md

## Scope
- Applies to `src/BlazorDevTools.Extension`.

## Focus
- panel tree/details UI
- content script behavior
- devtools page wiring
- inspect mode and picker behavior
- extension build/manifest/static assets

## Guidance
- Prefer fixing extension problems inside the extension unless the root cause clearly lives in runtime/protocol code.
- Keep the UI simple and developer-focused.
- Preserve:
  - tree rendering
  - search/filter
  - selection/details behavior
  - safe messaging without noisy console spam
- Validate with:
  - `npm ci`
  - `npm run build`
