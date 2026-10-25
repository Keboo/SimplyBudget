# Squad Team

> SimplyBudget

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Leia | Technical Lead | `.squad/agents/leia/charter.md` | Active |
| Threepio | Frontend Engineer | `.squad/agents/threepio/charter.md` | Active |
| Hans | Backend Engineer | `.squad/agents/hans/charter.md` | Active |
| Lando | Data Engineer | `.squad/agents/lando/charter.md` | Active |
| Luke | Quality Engineer | `.squad/agents/luke/charter.md` | Active |
| Scribe | Decision Merger | `.squad/agents/scribe/charter.md` | Built-in |
| Ralph | Work Monitor | `.squad/agents/ralph/charter.md` | Built-in |
| Rai | RAI Reviewer | `.squad/agents/rai/charter.md` | Built-in |
| Fact Checker | Verification & Devil's Advocate | `.squad/agents/fact-checker/charter.md` | Built-in |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes requiring cross-team discussion

## Project Context

- **Project:** SimplyBudget
- **Purpose:** Personal budgeting and expense tracking, including receipt uploads and pending expenses.
- **Stack:** C#, .NET 10, .NET Aspire, Vue/Vite, TypeScript, and .NET/UI test projects.
- **Created:** 2026-10-19
