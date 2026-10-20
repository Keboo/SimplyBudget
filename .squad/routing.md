# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture and cross-cutting technical decisions | Lead | System design, tradeoffs, technical direction |
| Vue frontend and user experience | Frontend | Views, components, accessibility, client-side state |
| .NET application logic and APIs | Backend | Business rules, services, endpoints |
| Persistence and data access | Data | Storage models, schema changes, queries |
| Automated quality and test coverage | Tester | Unit, integration, and UI tests |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **One accountable owner** — route each outcome to one primary specialist; add collaboration only for a concrete, bounded need.
2. **Scribe runs only when accepted durable decisions need merging**, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When multiple agents could handle it**, select the one whose domain is the primary concern.
5. **Split only independently deliverable outcomes** with distinct owners; otherwise the accountable owner coordinates bounded input.
6. **No anticipatory fan-out** — add reviewers or contributors only when policy or a specific need requires them.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
