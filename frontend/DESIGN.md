# Design direction

## Shape

The subject is workshop evidence: source statements, annotations, review marks,
and the thread that connects a claim to a decision. The interface therefore
uses an evidence workbench rather than a generic dashboard.

The compact token system is:

| Token | Value | Use |
|---|---|---|
| Deep ink | `#15313a` | Primary text, rules, controls |
| Oxide | `#a44427` | Decisions, evidence nodes, emphasis |
| Cyan | `#087786` | Focus, evidence thread, progress |
| Paper | `#f3efe2` | Work surfaces |
| Light paper | `#fbf8ed` | Reading surfaces |
| Moss | `#3e6352` | Confirmed state |

Typography uses local stacks only. Rockwell, Roboto Slab, and Courier New form
the display stack. Aptos, Trebuchet MS, and Segoe UI form the reading stack.
Consolas and Liberation Mono identify references, counts, and status labels.

Two layout ideas were compared:

```text
Option A: evidence workbench
┌──────────────────────────────────────────────────────────────┐
│ masthead                         connection state             │
├──────────────┬───────────────────────────────────────────────┤
│ capture form │  ● statement                                  │
│              │  │                                            │
│              │  ● source and validation                      │
└──────────────┴───────────────────────────────────────────────┘

Option B: generic dashboard
┌──────────────────────────────────────────────────────────────┐
│ hero metrics                                                 │
├───────────────────┬───────────────────┬──────────────────────┤
│ metric card       │ metric card       │ metric card          │
└───────────────────┴───────────────────┴──────────────────────┘
```

Option A was selected because the relationship between source, claim, and
decision is the product's central behavior. Option B was rejected because it
turns accountable evidence into interchangeable summary tiles.

The single signature element is the evidence thread: a cyan vertical rule with
oxide source nodes. It appears only where evidence is read, so it carries
meaning rather than becoming decoration.

## Build

The interface uses editorial rules, rectangular controls, wide reading measures,
and dense operational rows. Large headings establish hierarchy without a
promotional hero treatment. Role navigation names work that people recognize: Evidence, Decision review,
Outcomes, and Delivery documents.

## Naming

The naming mode is audience-led. Evidence to Decision states the product's
purpose in words that all four roles can repeat without learning an internal
category name. Navigation identifies the record or task each audience controls,
while actions use direct verbs such as Capture, Record, Review, Create, Save,
and Download.

The naming pass replaced broad labels such as Discover, Review, Handoff, and
Recommendation with Evidence, Decision review, Delivery documents, and Review
brief. It also replaced implementation-facing connection and generation terms
in empty, error, and status messages. No trademark, domain, or marketplace
availability checks were performed for the product name.

Responsive layouts collapse into one reading column. Wide tables are avoided.
Pointer targets remain at least 44 CSS pixels high, focus is visible, and native
elements preserve keyboard behavior.

## Critique

The first pass risked resembling a warm editorial template because it paired a
paper background with display type. The response was to remove high-contrast
serif styling, decorative shadows, oversized metric cards, and decorative
gradients. A sturdy slab and condensed local stack now evokes workshop labels
and marked-up working documents rather than a lifestyle publication.

The initial navigation also gave connection status too much visual weight. It
was reduced to a utility line so current work remains primary.

## Audit

The design avoids the requested Impeccable tells:

- no Inter dependency or remote font request
- no purple and blue gradient
- no nested card system
- no gray copy on colored surfaces
- no pure black palette
- no pill-shaped control language
- no bounce easing
- no decorative icon collection

Color never carries status alone. Every state includes text or a symbol with an
accessible label. Reduced-motion preferences stop progress animation. Error,
status, and form controls use semantic roles and associated labels.

## Harden

The shell fails closed when configuration or response validation fails. API
content is never cached for offline use. Evidence drafts remain in memory rather
than weakening the data-storage policy. Mutation controls require a current
ETag, and action buttons disable during requests to prevent duplicate work.

## Polish

The final pass removed the hero background pattern, shortened repeated copy,
removed decorative opportunity numbering, kept the evidence thread as the only
signature motif, and replaced generic cards with ruled rows. Mobile spacing,
long text wrapping, focus outlines, offline wording, and empty-state
instructions received a separate review.

## UI Design Brain review

### Job, shell, and context

The primary object is an engagement record. The stable shell now names the
organization reference, engagement reference, current audience role, connection
state, and last successful record check. The role label is orientation only.
Every sensitive read and write still goes to the authorized API; navigation
visibility is never treated as permission.

### Forms and concurrent edits

Evidence and decision controls are grouped with `fieldset` and `legend`.
Required fields have associated labels, linked inline errors, and a focused
error summary. A `409` or `412` response leaves all evidence and decision edits
in memory. Refresh actions fetch the new ETag and record version without
replacing the form component, then ask the reviewer to reassess and submit.
There is no silent overwrite or last-write-wins path.

### Provenance, freshness, and trust boundaries

The UI uses three visible content zones:

- application chrome contains product-owned navigation, context, and status
- workshop-record content labels participant or user-supplied statements and
  shows source, participant, capture time, modality, validation, confidence, and
  version where the API provides them
- AI-assisted and generated documents use a separate double-rule boundary,
  explicit disclosure, freshness, limitations, and required human approval

Review projections name their canonical record version. A mismatch blocks
submission until refresh. Delivery documents distinguish current, older, and
unverifiable source versions. Governance and missing-evidence blockers name what
must change rather than relying on color.

The recommendation operation returns a result reference, but the existing API
does not expose an endpoint that returns the stored recommendation body. The
completion view therefore does not invent recommendation text. It presents the
current linked evidence, known blockers, required reviewer action, and an
explicit notice that the completed brief cannot be opened in this interface.
Operation and source-version details remain behind disclosure.

### State review

| State | User-visible behavior | Recovery |
|---|---|---|
| Loading | Calm record-check message and textual progress | Wait |
| Empty | Names the missing record and prerequisite action | Capture or frame the prerequisite |
| Offline with loaded data | Keeps the in-memory record visible and marks changes paused | Reconnect; drafts remain in the tab |
| Offline at open | States that the engagement cannot be opened | Reconnect and retry |
| Permission denied | Names required access without a retry loop | Contact the engagement owner |
| Stale | Names both record versions and blocks mutation | Refresh while preserving edits |
| Conflict | Focused summary; no edit loss | Refresh, reassess, submit |
| Blocked | Names evidence or governance requirements | Resolve the listed items |
| Operation failure | Names that the brief was not added | Return to decision review |
| Generated document stale | Warns not to use the older document | Create a current document |

The application does not persist engagement data, decisions, or evidence in
browser storage. Offline capability covers the application shell and in-tab
draft continuity, not an unapproved local record cache.

### Accessibility and responsive review

Controls use native semantics, a logical keyboard order, visible focus, linked
errors, textual statuses, and 44-pixel minimum targets. Full-page failures move
focus to the heading; form failures move focus to the summary. Reduced-motion
preferences stop decorative progress movement, and forced-colors rules preserve
status boundaries. At narrow widths, shell context stacks, navigation remains
horizontally reachable, two-column workbenches become one column, provenance
grids collapse, and technical detail wraps rather than clipping.

Automated tests cover navigation, configuration, empty records, expired
sessions, permission denial, form errors, concurrency headers, conflict draft
preservation, recommendation accountability, runtime parsing, polling, and
cancellation. Keyboard-only, screen-reader, forced-colors, 200% zoom, and
translated-text expansion still require manual browser and assistive-technology
testing.

### Naming self-review

The follow-up pass retained the audience-led product, role, route, action,
status, and recovery names. New copy says “record,” “review brief,” “source,”
“decision reviewer,” and “refresh” instead of exposing worker, orchestration,
graph, model, or storage terms. Technical references appear only in optional
detail, while backend contract and route names remain unchanged.
