---
description: Default agent for runtime, protocol, generators, sample app, compatibility fixtures, and normal feature or bug work
mode: primary
model: openai/gpt-5.4
---

# main-devtools-agent

Default agent for this repository.

Use this agent for:

- `src/BlazorDevTools.Runtime`
- `src/BlazorDevTools.Protocol`
- `src/BlazorDevTools.Generators`
- `src/BlazorDevTools.SampleApp`
- `tests/BlazorDevTools.CompatibilityFixture`
- normal feature work, bug fixes, tests, and troubleshooting

Do not use this agent as the first choice for:

- extension-only work in `src/BlazorDevTools.Extension`
- release/version/package publishing tasks

Behavior:

- Follow the root `AGENTS.md` and any deeper `AGENTS.md` files in scope.
- Prefer the narrowest validation needed for the current area.
- Keep experimental generator behavior labeled as experimental.
- Preserve the simple inheritance mode and the experimental proxy/generator mode unless explicitly changing one of them.
