AI Opportunity Engineering System - v3.0

Status: Proposed final architecture
Version: 3.0
Purpose: A reusable, evidence-led system for turning customer problems into validated, measurable, trust-aware AI opportunities and engineering decisions.

1. Executive Summary

The AI Opportunity Engineering System is an outcome-first system for helping organisations discover, assess, validate, prioritise, and deliver AI-enabled opportunities.

It is not primarily an AI workshop.

It is a decision and engineering system whose first user interface happens to be a facilitated workshop.

The system combines five major ideas:

Evidence-led opportunity discovery
A canonical Opportunity Graph
A visual Card Engine
An assistive Facilitator Agent
A validation-to-outcome lifecycle

The core model is:

CUSTOMER REALITY
       ↓
    EVIDENCE
       ↓
    WORKFLOW
       ↓
     PROBLEM
       ↓
   OPPORTUNITY
       ↓
   INTERVENTION
       ↓
     CONCEPT
       ↓
   ASSUMPTIONS
       ↓
   EXPERIMENT
       ↓
     RESULT
       ↓
    DECISION
       ↓
      PILOT
       ↓
    OUTCOME
       ↓
 SCALE / REDESIGN / STOP

The Opportunity Graph provides the underlying system of record.

The Card Engine provides the human-facing interaction model.

The Facilitator Agent provides intelligence and assistance.

The Decision Engine provides structured evaluation.

The Validation Engine provides the bridge from uncertainty to evidence.

The system is intentionally technology-neutral.

Microsoft technologies can be exposed through technology adapters and implementation patterns without allowing current product capabilities to dictate the discovery methodology.

2. Product Thesis

AI transformation should not begin with:

"What can we build with AI?"

It should begin with:

"What should change, why does it matter, what evidence supports it, and what do we need to learn before investing?"

The system therefore optimises for decision quality, not idea quantity.

A successful engagement should leave the customer with:

fewer but better opportunities
evidence supporting each opportunity
explicit uncertainty
clear alternatives
measurable outcomes
identified owners
understood trust requirements
defined validation experiments
explicit decisions
delivery-ready next steps

The system must also make it acceptable to conclude:

Do not build this.

3. Product Positioning

The system should not be positioned as:

AI workshop software.

Nor:

AI idea-generation cards.

Preferred positioning:

An evidence-led AI opportunity engineering system that turns customer problems into validated, measurable engineering decisions.

Alternative short description:

From customer reality to validated AI opportunity.

In production, the system should be positioned commercially as:

A governed decision-support system for qualifying AI investments, reducing rediscovery in delivery, and making trust, evidence, and ownership explicit before build.

Primary production user:

Facilitator / Opportunity Lead

Primary production buyer:

Transformation, innovation, or business sponsor accountable for prioritised investment decisions.

Secondary production consumers:

Executive sponsors
Enterprise architects
Delivery leads
Security / privacy / governance reviewers

4. Core Product Model

The system consists of six layers.

┌─────────────────────────────────────────────┐
│                 EXPERIENCE                  │
│                                             │
│ Workshop / Teams / In-room / Executive     │
└──────────────────────┬──────────────────────┘
                       ↓
┌─────────────────────────────────────────────┐
│                 CARD ENGINE                 │
│                                             │
│ Browse / Filter / Match / Compare / Build  │
└──────────────────────┬──────────────────────┘
                       ↓
┌─────────────────────────────────────────────┐
│              OPPORTUNITY GRAPH              │
│                                             │
│ Evidence / Workflow / Problem / Opportunity │
│ Concept / Trust / Assumption / Experiment   │
│ Decision / Pilot / Outcome                  │
└──────────────────────┬──────────────────────┘
                       ↓
┌─────────────────────────────────────────────┐
│               DECISION ENGINE               │
│                                             │
│ Value / Confidence / Readiness / Trust      │
└──────────────────────┬──────────────────────┘
                       ↓
┌─────────────────────────────────────────────┐
│              VALIDATION ENGINE              │
│                                             │
│ Hypothesis / Experiment / Result / Learning │
└──────────────────────┬──────────────────────┘
                       ↓
┌─────────────────────────────────────────────┐
│               OUTCOME ENGINE                │
│                                             │
│ KPI / Baseline / Target / Result / Value    │
└─────────────────────────────────────────────┘

The Facilitator Agent operates across these layers.

5. Architectural Principle

The most important architectural rule is:

Cards are representations of domain objects, not the domain model itself.

For example:

Opportunity
    │
    ├── Domain representation
    │
    ├── JSON representation
    │
    ├── API representation
    │
    ├── Card representation
    │
    ├── Executive representation
    │
    └── Delivery representation

This allows the same opportunity to appear as:

a workshop card
a comparison table
an executive summary
a pilot brief
an architecture input
a decision record

without duplicating the underlying information.

6. The Opportunity Graph

The Opportunity Graph is the canonical representation of the engagement.

It connects evidence to decisions.

Example:

Evidence
   │
   ▼
Problem
   │
   ▼
Opportunity
   │
   ├──────────────┐
   ▼              ▼
Workflow       Outcome
   │              │
   ▼              ▼
Intervention    KPI
   │
   ▼
Concept
   │
   ├─────────────┬──────────────┐
   ▼             ▼              ▼
Assumption     Trust          Dependency
   │
   ▼
Experiment
   │
   ▼
Result
   │
   ▼
Decision
   │
   ▼
Pilot
   │
   ▼
Outcome

This graph is the fundamental differentiator.

7. Graph Principles

The graph MUST:

preserve provenance
preserve relationships
distinguish fact from interpretation
support versioning
support decision history
support uncertainty
support alternative concepts
support rejected concepts
support experiment results
support outcome measurement

The graph SHOULD:

support visual exploration
support filtering
support comparison
support recommendations
support cross-engagement analysis
8. Canonical Domain Objects

The system contains the following primary objects:

Engagement
Participant
Evidence
Workflow
Problem
Opportunity
Intervention
Concept
Assumption
Experiment
Trust Profile
Readiness Profile
KPI
Decision
Pilot
Outcome
Card
Source
Owner

Secondary objects include:

stakeholder
persona
JTBD
dependency
risk
constraint
technology pattern
industry pattern
business case
dissent
comment
artifact
9. Engagement Object

The Engagement represents the customer interaction.

Engagement
├── id
├── customer
├── sponsor
├── objectives
├── scope
├── participants
├── facilitators
├── date
├── methodology version
├── source versions
├── opportunities
├── decisions
└── outcomes

The Engagement is the top-level container for the Opportunity Graph.

10. Evidence Object

Evidence is a first-class object.

Evidence
├── id
├── type
├── source
├── participant
├── timestamp
├── statement
├── interpretation
├── confidence
├── validation status
├── related workflow
└── related opportunities
11. Evidence Types

The system MUST distinguish:

Observed

Directly observed behaviour or workflow.

Measured

Quantified evidence.

Customer Statement

A participant assertion.

External Evidence

Document, system record, research, or other source.

Interpretation

Facilitator or agent interpretation.

Assumption

An unverified claim.

Hypothesis

A proposed explanation or expected result.

These must never be silently merged.

12. Evidence Provenance

Every evidence item should retain:

source
author/participant where applicable
timestamp
original wording
normalized interpretation
associated artifact
validation status

The original statement should remain recoverable.

Example:

SOURCE:


"We spend most mornings looking for information."


INTERPRETATION:


Information retrieval appears to be a material
component of case preparation.


STATUS:


Unverified.
13. Evidence Strength

Evidence strength is qualitative.

VERY STRONG
Measured / directly observed


STRONG
Multiple independent sources


MODERATE
Single reliable source


WEAK
Unverified participant assertion


HYPOTHETICAL
Assumption / hypothesis

Evidence strength influences confidence but should not become a simplistic additive score.

14. Evidence Conflicts

Contradictions must be preserved.

Example:

Claim A:
Preparation takes 30 minutes.


Claim B:
Preparation takes 2 hours.

The system creates:

Evidence Conflict
├── Claim A
├── Claim B
├── Sources
├── Potential explanations
└── Validation experiment

The system MUST NOT silently average contradictory claims.

15. Workflow Object

A Workflow describes where work happens.

Workflow
├── trigger
├── actors
├── inputs
├── steps
├── decisions
├── systems
├── handoffs
├── exceptions
├── outputs
└── outcome

A workflow is normally represented visually.

16. Workflow Cards

Workflow cards provide a compact view of important workflow stages.

Example:

┌─────────────────────────────┐
│ ⚙ WORKFLOW STEP             │
│                             │
│ Prepare customer case       │
│                             │
│ Actor                       │
│ Claims Specialist           │
│                             │
│ Systems                     │
│ CRM / Documents / Email     │
│                             │
│ Pain                        │
│ Context gathering           │
│                             │
│ Evidence                    │
│ ●●● Strong                  │
└─────────────────────────────┘
17. Problem Object

Canonical structure:

Problem
├── user
├── workflow
├── goal
├── constraint
├── impact
├── evidence
└── confidence

Problem statement:

[User] needs to [goal], but [constraint], causing [impact].

18. Opportunity Object

The Opportunity is the primary object in the system.

Opportunity
├── identity
├── problem
├── workflow
├── user
├── evidence
├── desired outcome
├── KPI
├── baseline
├── value
├── confidence
├── readiness
├── trust
├── interventions
├── concepts
├── assumptions
├── experiments
├── decisions
├── owner
└── lifecycle state
19. Opportunity Lifecycle
DISCOVERED
    ↓
FRAMED
    ↓
EVIDENCE_REVIEWED
    ↓
SHORTLISTED
    ↓
CONCEPTUALISED
    ↓
VALIDATING
    ↓
DECIDED
    ↓
PILOTING
    ↓
MEASURING
    ↓
OUTCOME_RECORDED

Terminal outcomes:

SCALED
REDESIGN_REQUIRED
STOPPED
PARKED
REJECTED
20. Intervention Object

An intervention represents the simplest plausible mechanism for changing the problem.

Types:

PROCESS_REDESIGN
POLICY_CHANGE
ROLE_CHANGE
KNOWLEDGE_IMPROVEMENT
DATA_IMPROVEMENT
ANALYTICS
AUTOMATION
AI_ASSISTANCE
COPILOT
DECISION_SUPPORT
AGENT
BOUNDED_AUTONOMY
NO_ACTION

The system MUST compare alternatives before assuming AI is appropriate.

21. Concept Object

A Concept describes a specific implementation direction.

Concept
├── intervention
├── capability
├── user experience
├── workflow change
├── technology pattern
├── autonomy
├── trust
├── dependencies
├── assumptions
├── value hypothesis
└── validation plan

Multiple concepts may exist for one opportunity.

22. Alternative Concepts

An opportunity should support competing concepts.

Example:

Opportunity:
Reduce research effort.


Concept A:
Knowledge consolidation.


Concept B:
AI-assisted retrieval.


Concept C:
Automated workflow.


Concept D:
Process redesign.

This prevents solution fixation.

23. Concept Comparison

Concepts should be directly comparable.

                  A          B          C          D


Value             High       High       Medium     Medium
Confidence        High       Medium     Low        High
Complexity        Low        Medium     High       Low
Trust             Low        Medium     High       Low
Time to value     Fast       Medium     Slow       Fast


Decision           Pilot      Validate   Research   Pilot
24. Card Engine

The Card Engine is the primary visual interaction layer.

It provides:

card creation
rendering
filtering
grouping
sorting
pinning
comparison
matching
linking
promotion
lifecycle state
evidence indicators

The card engine should support both workshop facilitation and individual exploration.

25. Card Philosophy

Cards should be:

visually distinctive
information-dense
quickly scannable
comparable
practical
explainable
progressive

A card should answer:

What is this?

Why does it matter?

How certain are we?

What is blocking it?

What should happen next?

26. Card Taxonomy

Core card types:

PROBLEM
PERSONA
WORKFLOW
EVIDENCE
OPPORTUNITY
INTERVENTION
CONCEPT
ASSUMPTION
EXPERIMENT
TRUST
READINESS
KPI
DECISION
PILOT
OUTCOME
27. Card Anatomy

A standard card contains:

┌─────────────────────────────────────┐
│ ICON  TYPE                          │
│                                     │
│ TITLE                               │
│                                     │
│ Short description                   │
│                                     │
│ ┌─────────┐ ┌─────────┐             │
│ │ VALUE   │ │ CONF.   │             │
│ └─────────┘ └─────────┘             │
│                                     │
│ Key facts                            │
│                                     │
│ Evidence / Trust / Readiness badges │
│                                     │
│ ─────────────────────────────────── │
│                                     │
│ NEXT ACTION                         │
└─────────────────────────────────────┘
28. Card Density

Cards should support three levels.

Compact

For browsing and large collections.

Standard

For workshop use.

Expanded

For detailed decision-making.

Example:

Compact
   ↓
Standard
   ↓
Expanded

The user should not have to leave the card environment simply to inspect more information.

29. Progressive Disclosure

The card should expose only what is needed at each level.

Compact:

title
type
value
confidence

Expanded:

evidence
workflow
assumptions
trust
owner
decision rationale

This keeps the workshop visually manageable.

30. Evidence Badges

Every relevant card should expose evidence status.

Example:

Evidence


●●● Strong
●●○ Moderate
●○○ Weak
○○○ Hypothesis

More detailed:

3 Observed
2 Measured
1 Statement
2 Assumptions
31. Trust Badges

Example:

TRUST


✓ Human review
✓ Audit trail
⚠ Sensitive data
⚠ Privacy assessment

Trust status should be immediately visible.

32. Readiness Badges

Example:

READINESS


✓ Owner
✓ KPI
✓ Data
⚠ Integration
✗ Governance

This makes blockers visible before the customer reaches the decision stage.

33. Lifecycle Badges

Cards should visually communicate lifecycle state.

DISCOVERY
SHORTLISTED
VALIDATING
PILOT
MEASURING
SCALED
PARKED
REJECTED
34. Card Promotion

Cards should be promotable.

Example:

Problem Card
      ↓
Opportunity Card
      ↓
Concept Card
      ↓
Validation Card
      ↓
Pilot Card
      ↓
Outcome Card

The object identity should remain consistent.

This is not creating a new unrelated record.

It is changing the representation and maturity of the same opportunity.

35. Card Relationships

Cards should be connectable.

Example:

[EVIDENCE]
     │
     ▼
[PROBLEM]
     │
     ▼
[OPPORTUNITY]
   ┌─┴───────┐
   ▼         ▼
[CONCEPT A] [CONCEPT B]
   │
   ▼
[EXPERIMENT]
   │
   ▼
[DECISION]

Users should be able to navigate these relationships.

36. Card Filtering

The system should support filtering by:

card type
industry
workflow
user
value
confidence
trust
readiness
lifecycle
owner
evidence strength
intervention
technology pattern
37. Card Matching

The system should support:

Find an intervention

or:

Find an opportunity pattern

The user supplies requirements.

The system returns candidate cards ranked by fit.

38. Match Explanation

Every recommendation must be explainable.

Recommendations are decision-support hypotheses, not decisions.

Each recommendation should expose:

matching dimensions used
source evidence used
missing information
confidence limitations
review owner

Example:

Match: HIGH


Why:
✓ Same workflow pattern
✓ Same desired outcome
✓ Suitable autonomy
✓ Compatible trust posture
⚠ Data dependency remains

The system MUST NOT present unexplained recommendation percentages as objective truth.

The system MUST abstain or downgrade the recommendation when evidence is incomplete, stale, or contradictory.

39. Card Comparison

Users can pin cards and compare them.

Comparison dimensions include:

value
confidence
evidence
trust
readiness
complexity
time to value
autonomy
KPI
owner
dependencies
40. Card Clustering

Participants can cluster cards around:

workflow
user
problem
value
theme
strategic priority

Clustering should create relationships in the graph rather than simply moving visual objects.

41. Card Voting

Voting may be used for:

perceived value
urgency
user pain
strategic importance

Votes are treated as participant signals, not objective evidence.

This distinction is critical.

42. Card Selection

The system should distinguish:

Participant preference

"We like this."

from:

Evidence-supported opportunity

"Evidence indicates this is a material problem."

Both can be valuable.

They must not be conflated.

43. Facilitator Agent

The Facilitator Agent operates over the Opportunity Graph.

It can:

summarize
ask questions
recommend cards
detect gaps
identify contradictions
suggest interventions
challenge assumptions
draft decisions
propose experiments
prepare outputs

It cannot silently make consequential decisions.

44. Agent Context

The agent should receive structured context:

Engagement
Workflow
Evidence
Current opportunity
Related cards
Current workshop state
Trust
Readiness
Assumptions
Previous decisions

The agent should not rely solely on conversation history.

45. Agent Modes
DISCOVER
FRAME
CHALLENGE
IDEATE
COMPARE
PRIORITISE
VALIDATE
DECIDE
HANDOFF
MEASURE
46. Agent Guardrails

The agent MUST:

identify unsupported claims
distinguish evidence from assumptions
cite source evidence internally
expose uncertainty
preserve contradictions
avoid invented customer facts
avoid making customer commitments
respect human authority

The agent SHOULD:

ask the smallest useful question
minimise interruption
favour evidence over speculation
propose simpler interventions
identify missing owners and KPIs
47. Agent Challenge Behaviour

The agent should actively challenge premature conclusions.

Example:

Participant:

"We should build an autonomous agent."

Agent:

"Before selecting the agent pattern, what workflow step is currently causing the problem, and what evidence shows it is material?"

This is a core product behaviour.

48. Decision Engine

The Decision Engine evaluates opportunities.

Primary dimensions:

VALUE
CONFIDENCE
TRUST
READINESS
FEASIBILITY
TIME_TO_VALUE

These are not necessarily collapsed into one score.

49. Value

Value considers:

business impact
customer impact
user impact
risk reduction
strategic relevance
revenue
cost
throughput
quality
50. Confidence

Confidence considers:

evidence strength
workflow understanding
feasibility evidence
data readiness
owner commitment
KPI clarity
trust confidence
51. Readiness

Readiness includes:

Owner
KPI
Data
Process stability
Integration
Governance
Security
Change capacity
52. Decision Matrix

The primary visual model remains:

                  HIGH VALUE
                      │
              VALIDATE│ PILOT
                      │
──────────────────────┼────────────────────
                      │
              RESEARCH│ QUICK WIN
                      │
                  LOW VALUE


       LOW CONFIDENCE → HIGH CONFIDENCE

But the system must also apply trust and readiness gates.

53. Decision Classes
PILOT_NOW
VALIDATE
SHAPE_NEXT
PREREQUISITES_REQUIRED
PROCESS_REDESIGN
TRUST_BLOCKED
DATA_BLOCKED
PARK
REJECT
54. Decision Gates

Example:

High value
+
Strong evidence
+
Known owner
+
Measurable KPI
+
Acceptable trust
+
Sufficient readiness
=
PILOT_NOW

Where:

High value
+
Weak evidence
=
VALIDATE

And:

High value
+
Unstable process
=
PROCESS_REDESIGN
55. Trust Engine

Trust should be evaluated continuously.

Dimensions:

privacy
security
IP
regulation
user impact
decision impact
data sensitivity
auditability
human oversight
model risk
operational risk
56. Autonomy Model
A0 Human only


A1 AI informs


A2 AI recommends


A3 AI recommends + human approves


A4 AI executes bounded actions


A5 Autonomous execution

Increasing autonomy requires increasing evidence and controls.

57. Human Accountability

Every consequential AI workflow must identify:

accountable owner
decision owner
approval point
escalation path
human review
prohibited delegation
58. Assumption Engine

The system automatically surfaces assumptions.

Example:

ASSUMPTION


Specialists spend >20% of preparation
time searching for information.


Evidence:
Weak


Impact if false:
High


Recommended action:
Measure across 20 cases.
59. Validation Engine

Validation turns uncertainty into learning.

Assumption
   ↓
Hypothesis
   ↓
Experiment
   ↓
Measurement
   ↓
Result
   ↓
Confidence update
   ↓
Decision
60. Experiment Object
Experiment
├── assumption
├── hypothesis
├── objective
├── method
├── sample
├── metric
├── expected result
├── actual result
├── interpretation
├── confidence change
└── decision impact
61. Experiment Cards

Example:

┌──────────────────────────────┐
│ 🧪 EXPERIMENT                │
│                              │
│ Measure search effort        │
│                              │
│ Assumption                   │
│ Search is >20% of effort     │
│                              │
│ Sample                       │
│ 20 cases                     │
│                              │
│ Metric                       │
│ Minutes spent searching      │
│                              │
│ Success                      │
│ >20%                         │
│                              │
│ [ RUN EXPERIMENT ]           │
└──────────────────────────────┘
62. KPI Engine

Every pilot-ready opportunity requires:

baseline
target
measurement
owner
period

Example:

Baseline: 42 minutes
Target: 30 minutes
Measurement: case telemetry
Owner: Operations Lead
Period: 4 weeks
63. Outcome Engine

Outcomes are recorded after validation/pilot.

Outcome
├── baseline
├── target
├── actual
├── measurement method
├── period
├── confidence
├── unintended consequences
├── adoption
├── trust incidents
└── recommendation
64. Outcome States
SUCCESS
PARTIAL_SUCCESS
NO_MEANINGFUL_CHANGE
FAILED
STOPPED
REDESIGN_REQUIRED
SCALED
65. Pilot Card

Example:

┌────────────────────────────────────┐
│ 🚀 PILOT                           │
│                                    │
│ Case Research Assistant            │
│                                    │
│ VALUE          CONFIDENCE          │
│ HIGH           HIGH                │
│                                    │
│ KPI                                  │
│ Preparation time                   │
│                                    │
│ 42 min → 30 min                    │
│                                    │
│ TRUST                              │
│ ✓ Human approval                   │
│ ✓ Audit logging                    │
│                                    │
│ OWNER                              │
│ Claims Operations                  │
│                                    │
│ [ PILOT READY ]                    │
└────────────────────────────────────┘
66. Outcome Card

After measurement:

┌────────────────────────────────────┐
│ 📈 OUTCOME                         │
│                                    │
│ Case Research Assistant            │
│                                    │
│ Target       Actual                │
│ 30 min       28 min                │
│                                    │
│ Result                             │
│ ✓ SUCCESS                          │
│                                    │
│ Adoption                           │
│ 84%                                │
│                                    │
│ Recommendation                    │
│ SCALE                              │
└────────────────────────────────────┘
67. Workshop Experience

The workshop should become a live graph-building experience.

The workshop is a first-class entry point, but not the only operating mode.

The same graph, decisions, and artifacts must also support asynchronous review by sponsors, architects, delivery teams, and governance reviewers between live sessions.

Participants do not merely fill out forms.

They:

Explore cards
    ↓
Select cards
    ↓
Connect cards
    ↓
Attach evidence
    ↓
Create opportunities
    ↓
Compare alternatives
    ↓
Identify assumptions
    ↓
Choose experiments
    ↓
Make decisions
68. Workshop State Machine
SETUP
  ↓
CONTEXT
  ↓
WORKFLOW
  ↓
EVIDENCE
  ↓
PROBLEMS
  ↓
OPPORTUNITIES
  ↓
INTERVENTIONS
  ↓
CONCEPTS
  ↓
VALIDATION
  ↓
PRIORITISATION
  ↓
DECISION
  ↓
HANDOFF
69. 90-Minute Workshop
0-10

Outcome and context.

10-25

Workflow mapping.

25-40

Evidence and problems.

40-55

Opportunity creation.

55-70

Intervention and concept cards.

70-82

Comparison and prioritisation.

82-90

Decision and next actions.

70. Card-Based Workshop Mechanics

Participants should be able to:

drag
pin
compare
cluster
connect
vote
annotate
challenge
promote
reject

Every interaction should ultimately modify the graph.

71. "Find a Match" Experience

A central interaction should be:

What pattern matches this problem?

Inputs:

workflow
problem
outcome
constraints
trust
autonomy
readiness

Outputs:

Top matches
Alternative matches
Why they match
Why they don't
Unknowns
Recommended validation
72. Explainable Matching

Matching should use explicit dimensions.

In production, matching should begin with curated rules and transparent heuristics before introducing more complex ranking models.

The matching service should prefer to say:

Insufficient evidence to recommend confidently.

rather than force a weak recommendation.

Example:

Workflow match       5/5
Outcome match        5/5
User match           4/5
Trust match          4/5
Readiness match      3/5
Autonomy match       5/5


Overall:
Strong candidate

The system should show the reasoning rather than only the result.

73. Search and Discovery

The Card Engine should support natural-language discovery.

Example:

Find patterns for reducing specialist research time in a sensitive workflow where humans must approve the result.

The system should return:

relevant opportunity patterns
intervention cards
concept cards
trust patterns
validation experiments
74. Card Library

The reusable library should include:

Problem patterns
User patterns
Workflow patterns
Intervention patterns
AI patterns
Trust patterns
Readiness patterns
Experiment patterns
KPI patterns
Industry patterns
75. Card Provenance

Every reusable card should include:

Card
├── id
├── type
├── title
├── description
├── source
├── source version
├── author
├── tags
├── industry
├── workflow patterns
├── evidence basis
├── last reviewed
└── lifecycle
76. Source Strategy

The existing Microsoft discovery-card material remains a canonical source where appropriate.

The system should:

preserve original assets
normalize metadata
retain provenance
version derived content
avoid silent modification

The card library should support multiple source collections over time.

77. Technology Adapter Layer

Technology mapping belongs outside the core method.

Opportunity
      ↓
Capability
      ↓
Intervention
      ↓
Architecture Pattern
      ↓
Technology Adapter

Potential adapters:

Microsoft Foundry
Copilot Studio
Microsoft 365 Copilot
Azure
Fabric
GitHub
conventional application development

The core opportunity model must remain independent of these products.

78. Microsoft Adapter

The Microsoft adapter may provide:

service mappings
architecture patterns
security considerations
governance considerations
implementation examples
capability references

It should never imply:

"Microsoft product X is the answer."

Instead:

"This intervention can potentially be implemented using X."

79. Agent Architecture

The initial implementation should use a single Facilitator Agent.

Potential future specialist agents:

Evidence Analyst
Workflow Analyst
Opportunity Analyst
Trust Analyst
Validation Planner
Business Case Analyst
Delivery Translator

These should only be introduced when evaluation demonstrates that specialist separation provides meaningful benefit.

80. Agent Tool Model

The Facilitator Agent should have tools for:

get_engagement
get_workflow
search_cards
get_card
create_card
link_cards
get_evidence
create_evidence
create_opportunity
compare_opportunities
create_assumption
create_experiment
update_confidence
create_decision
generate_handoff

Tool calls should modify the graph rather than merely generate prose.

81. Agent State Awareness

The agent must know:

workshop state
active opportunity
selected cards
evidence
unresolved conflicts
assumptions
trust blockers
readiness blockers
decisions
82. Agent Failure Behaviour

If the agent cannot determine something:

It should say:

"I don't have enough evidence to determine this."

not invent an answer.

If two sources conflict:

"The evidence conflicts. Here are the two claims."

If a solution is premature:

"The workflow and outcome are not yet sufficiently defined."

83. Human Override

Humans can:

override recommendations
change scores
reject concepts
change decisions
add evidence
remove evidence
mark assumptions as validated
change lifecycle state

The system records overrides.

84. Decision History

Every material decision should preserve:

previous state
new state
actor
timestamp
rationale
evidence
dissent
confidence
affected assumptions

This creates an auditable decision trail.

85. Dissent Model

Important disagreement should be captured.

Example:

Decision:
VALIDATE


Dissent:
Operations lead believes the baseline is overstated.


Impact:
Requires additional measurement.

Dissent is not treated as failure.

It is information about uncertainty.

86. Executive Experience

Executives should not need to navigate the full graph.

Executive consumption should work asynchronously and should not require attendance in the live workshop.

The system should generate:

Outcome
↓
Opportunity
↓
Value
↓
Confidence
↓
Trust
↓
Decision
↓
Investment
↓
Next action

The detailed evidence remains accessible.

87. Delivery Handoff

The system should generate delivery-ready artifacts.

For a pilot:

Pilot Brief
├── problem
├── workflow
├── users
├── outcome
├── KPI
├── baseline
├── target
├── concept
├── scope
├── trust
├── autonomy
├── dependencies
├── assumptions
├── experiment
├── owner
└── decision
88. Architecture Handoff

Architecture teams should receive:

workflow
systems
data
integrations
security requirements
trust requirements
autonomy
non-functional requirements
expected scale
KPI
operational requirements

The system should not automatically produce a final architecture without engineering validation.

89. Production Readiness

Before production, the opportunity should progress through:

Concept
 ↓
Validation
 ↓
Pilot
 ↓
Operational validation
 ↓
Trust approval
 ↓
Production readiness
 ↓
Production
90. Production Readiness Card

Example:

┌─────────────────────────────┐
│ 🏭 PRODUCTION READINESS     │
│                             │
│ ✓ Security                  │
│ ✓ Privacy                   │
│ ✓ Observability             │
│ ✓ Human oversight           │
│ ✓ Rollback                  │
│ ✓ KPI                       │
│ ⚠ Scale validation          │
│                             │
│ Status                      │
│ CONDITIONAL                 │
└─────────────────────────────┘
91. Evaluation Framework

The system itself must be measurable.

Method

Measure:

decision quality
opportunity quality
evidence completeness
facilitator effectiveness
Agent

Measure:

factuality
evidence attribution
hallucination rate
recommendation quality
challenge usefulness
Card Engine

Measure:

card comprehension
selection accuracy
comparison usefulness
navigation efficiency
participant engagement
Delivery

Measure:

workshop-to-pilot conversion
rework
time to validation
time to pilot
outcome attainment
92. Card UX Evaluation

Specifically test:

Can users understand a card in <10 seconds?
Can users compare three cards without explanation?
Can users identify uncertainty?
Can users understand why something is recommended?
Can users tell what action to take?
Can users distinguish evidence from opinion?

These are critical UX metrics.

93. Adversarial Scenarios

The system must test:

Unsupported ROI claims.
Conflicting participant statements.
AI solution fixation.
High-value/low-confidence opportunities.
Low-value/high-confidence opportunities.
Missing owners.
Missing KPIs.
Sensitive data.
Regulated workflows.
Unstable processes.
Agent failure.
Incorrect card recommendations.
Misleading participant voting.
Stale card content.
Technology availability mismatch.
94. Card Safety Principle

A visually polished card must never imply certainty that the underlying evidence does not support.

For example:

Bad:

ROI: $10M

when the only evidence is a participant estimate.

Better:

Potential value:
HIGH


Evidence:
Customer estimate - unvalidated


Validation:
Build baseline
95. No False Precision

Scores should be bounded.

Prefer:

LOW
MEDIUM
HIGH

or:

1-5 with explanation

rather than:

87.43% opportunity score

unless there is a defensible quantitative basis.

96. Industry Packs

Industry overlays can provide:

workflows
terminology
common risks
opportunity patterns
KPI patterns
trust considerations
examples

Initial candidates:

manufacturing
financial services
public sector
retail
healthcare

The core model remains unchanged.

97. Repository Architecture

Recommended repository:

ai-opportunity-engineering/
│
├── README.md
│
├── product/
│   ├── positioning.md
│   ├── personas.md
│   ├── operating-model.md
│   └── commercial-packaging.md
│
├── method/
│   ├── principles.md
│   ├── lifecycle.md
│   ├── facilitation.md
│   └── decision-method.md
│
├── domain/
│   ├── opportunity.md
│   ├── evidence.md
│   ├── workflow.md
│   ├── concept.md
│   ├── experiment.md
│   ├── trust.md
│   ├── decision.md
│   └── outcome.md
│
├── schemas/
│   ├── engagement.schema.json
│   ├── evidence.schema.json
│   ├── workflow.schema.json
│   ├── opportunity.schema.json
│   ├── intervention.schema.json
│   ├── concept.schema.json
│   ├── assumption.schema.json
│   ├── experiment.schema.json
│   ├── decision.schema.json
│   ├── pilot.schema.json
│   ├── outcome.schema.json
│   └── card.schema.json
│
├── cards/
│   ├── problems/
│   ├── users/
│   ├── workflows/
│   ├── interventions/
│   ├── concepts/
│   ├── trust/
│   ├── readiness/
│   ├── experiments/
│   ├── kpis/
│   └── industries/
│
├── card-engine/
│   ├── specification.md
│   ├── matching.md
│   ├── comparison.md
│   ├── lifecycle.md
│   └── ux.md
│
├── agent/
│   ├── system-instructions.md
│   ├── tools.md
│   ├── modes.md
│   ├── guardrails.md
│   └── evaluations/
│
├── technology/
│   ├── microsoft/
│   ├── patterns/
│   └── adapters/
│
├── operations/
│   ├── identity-and-access.md
│   ├── tenancy-and-isolation.md
│   ├── retention-and-deletion.md
│   ├── observability.md
│   └── backup-and-recovery.md
│
├── governance/
│   ├── data-policy.md
│   ├── card-review-policy.md
│   ├── agent-policy.md
│   └── approval-model.md
│
├── facilitation/
│   ├── 60-minute.md
│   ├── 90-minute.md
│   ├── 120-minute.md
│   ├── in-room.md
│   ├── teams.md
│   ├── hybrid.md
│   └── asynchronous-review.md
│
├── templates/
│   ├── opportunity.md
│   ├── experiment.md
│   ├── decision.md
│   ├── pilot.md
│   └── executive.md
│
├── evaluation/
│   ├── method/
│   ├── agent/
│   ├── cards/
│   └── adversarial/
│
├── engagements/
│   └── examples/
│
└── docs/
    ├── architecture.md
    ├── governance.md
    ├── roadmap.md
    ├── product-strategy.md
    └── production-readiness.md
98. Data Separation

The repository must distinguish:

Canonical source

External/reference material.

Reusable cards

Generalized knowledge.

Method

How the system is run.

Customer data

Engagement-specific information.

Evaluation

Test data and results.

Customer information must never accidentally become reusable card content.

99. Production Core Platform Scope

The initial production core platform should include:

Domain
Opportunity Graph
Evidence
Workflow
Opportunity
Concept
Assumption
Experiment
Decision
Experience
Card Engine
Browse
Filter
Compare
Pin
Connect
Promote
Agent
Discovery
Challenge
Compare
Prioritise
Validate
Handoff
Production operating modes
facilitated workshop mode
asynchronous review mode
executive review mode
digital card workspace
evidence capture
Outputs
opportunity portfolio
decision record
experiment
pilot brief
executive summary
Operational controls
identity integration
role-based access
audit trail
retention and deletion policy
customer workspace isolation
100. Explicitly Out of Scope for Initial Production Core

Do not build:

autonomous facilitation
autonomous customer decisions
complex multi-agent orchestration
automated production architecture
automatic ROI calculations
enterprise benchmarking
large analytics platform
deep bi-directional delivery-system integration
unbounded cross-customer data reuse
101. Production Technology Approach

Start with:

Markdown
JSON
Git
PowerPoint
Teams
Whiteboard
production web UI
Facilitator Agent
enterprise identity integration
observability and audit logging

The Opportunity Graph can initially be represented as JSON documents backed by controlled storage and auditable APIs.

Do not introduce a graph database until actual usage demonstrates the need.

102. Why JSON First

A JSON-based graph provides:

version control
portability
easy testing
simple schemas
easy agent tooling
low infrastructure overhead

Example:

{
  "opportunity_id": "OPP-001",
  "title": "Reduce case preparation time",
  "evidence": [
    "EVD-001",
    "EVD-004"
  ],
  "workflow": "WF-002",
  "concepts": [
    "CON-001",
    "CON-002"
  ],
  "value": "high",
  "confidence": "medium",
  "decision": "validate"
}
103. Real Engagement Validation

Before building substantial software:

Run at least five engagements.

Capture:

card usage
card confusion
comparison behaviour
agent interventions
evidence gaps
participant behaviour
facilitator friction
output usefulness
delivery-team feedback
104. Product Learning Loop
Engagement
    ↓
Telemetry / Feedback
    ↓
Observed Friction
    ↓
Method Change
    ↓
Card Change
    ↓
Agent Change
    ↓
Evaluation
    ↓
Next Engagement

The system itself should continuously improve.

105. Product Metrics

Primary metrics:

Evidence provenance coverage

How much of the opportunity portfolio is backed by attributable evidence?

Decision traceability

How many prioritised opportunities have explicit rationale, owner, and approval path?

Validated-decision cycle time

How quickly does an opportunity move from discovery to an evidence-backed decision?

Pilot conversion quality

How many pilot decisions progress with measurable baseline, target, and trust approval?

Outcome attainment

How many pilots produce measurable improvement?

Rediscovery reduction

How much discovery must engineering repeat?

106. North Star Metric

The strongest overall metric is:

Percentage of prioritised opportunities that reach a measurable, evidence-backed decision with explicit trust and ownership controls without requiring substantial rediscovery.

This is better than:

number of cards
number of ideas
number of workshops
number of AI concepts
107. Secondary Metrics

Track:

time from discovery to decision
time from decision to validation
time from validation to pilot
percentage with measurable baseline
percentage with owner
percentage with trust assessment
percentage with explicit decision rationale
percentage rejected before build
facilitator satisfaction
customer satisfaction
engineering satisfaction
governance-review turnaround
executive review completion
108. Portfolio Intelligence

After sufficient engagements, aggregate data.

Potential insights:

Most common problems
Most successful interventions
Common trust blockers
Common data blockers
Average validation time
Pilot conversion
Outcome attainment
Repeated workflow patterns

This becomes a potential long-term competitive advantage.

109. Reusable Pattern Intelligence

Over time, the system can learn:

"This workflow pattern repeatedly maps to these intervention patterns."

But recommendations must remain evidence-backed.

Historical frequency is not proof that a pattern is appropriate for the current customer.

110. Future Application

A future web application could look like:

┌──────────────────────────────────────────────────────┐
│ AI OPPORTUNITY ENGINEERING                           │
├──────────────────────────────────────────────────────┤
│                                                      │
│ [ Search cards... ]        [ Find a match ]          │
│                                                      │
│ FILTERS                                              │
│ Value  Confidence  Trust  Workflow  Industry        │
│                                                      │
│ ┌─────────┐ ┌─────────┐ ┌─────────┐                 │
│ │ Problem │ │ Problem │ │ Problem │                 │
│ │ Card    │ │ Card    │ │ Card    │                 │
│ └─────────┘ └─────────┘ └─────────┘                 │
│                                                      │
│              [ PINNED: 3 ]                           │
│                                                      │
│ [ Compare ] [ Create opportunity ]                   │
└──────────────────────────────────────────────────────┘

Selecting an opportunity opens:

┌──────────────────────────────────────────────────────┐
│ OPPORTUNITY                                           │
│                                                      │
│ Reduce case preparation effort                       │
│                                                      │
│ Evidence → Workflow → Problem → Concepts             │
│                                                      │
│ VALUE       CONFIDENCE      TRUST       READINESS     │
│ HIGH        MEDIUM          MEDIUM      HIGH          │
│                                                      │
│ ───────────────────────────────────────────────────  │
│                                                      │
│ Evidence                                             │
│ ●●● Strong                                           │
│                                                      │
│ Concepts                                             │
│ [AI Retrieval] [Knowledge Improvement] [Automation] │
│                                                      │
│ Assumptions                                          │
│ [2 unresolved]                                       │
│                                                      │
│ Decision                                             │
│ VALIDATE                                             │
│                                                      │
│ [Create experiment]                                  │
└──────────────────────────────────────────────────────┘
111. The Card as a Decision Surface

This is a central design principle.

The card is not just:

a prettier sticky note.

It is a decision surface.

A good card lets a participant understand:

WHAT
WHY
EVIDENCE
VALUE
CONFIDENCE
RISK
BLOCKERS
NEXT ACTION

within seconds.

112. Card Design Principles

The visual design should favour:

strong hierarchy
restrained colour
consistent iconography
meaningful badges
readable typography
clear state
minimal decoration
high information density
obvious actions

Cards should feel like professional engineering/product objects, not generic AI-generated UI.

113. Card Colour Semantics

Colour should communicate state rather than arbitrary aesthetics.

For example:

Neutral     = information
Positive    = validated / ready
Warning     = uncertainty / blocker
Critical    = trust / risk issue

Do not use colour as the only indicator.

Icons and text should reinforce meaning.

114. Accessibility

The Card Engine MUST support:

keyboard navigation
screen readers
sufficient contrast
non-colour state indicators
focus states
responsive layouts
readable text at workshop scale
115. Responsive Experience

Cards should work on:

large displays
laptops
tablets
Teams screens
projector displays

Workshop mode should optimise for group visibility.

Individual mode should optimise for exploration.

116. Card Animation

Animation may be used for:

promotion
connection
selection
comparison
lifecycle transitions

Animation must not interfere with facilitation.

The system should prefer subtle transitions over gamified effects.

117. Card Export

Cards should be exportable as:

PNG
SVG
PowerPoint
PDF
Markdown
JSON

The canonical source remains the graph object.

118. Card Printability

Cards should optionally support physical printing.

This is important because the system must remain useful if:

connectivity fails
the agent fails
a customer prefers physical workshops
the environment does not permit digital collaboration
119. Offline Facilitation

A future offline mode should allow:

card browsing
card selection
manual opportunity creation
local graph editing
export/sync later

This is a resilience feature rather than an initial priority.

120. Security

Customer data should be:

minimally collected
appropriately protected
access controlled
separated from reusable assets
auditable
isolated by customer workspace or tenant boundary
encrypted in transit and at rest

The production system should enforce:

enterprise identity integration
role-based access control
least-privilege agent access
audit logging for sensitive actions
secrets isolation

121. Privacy

The system should distinguish:

Reusable knowledge
vs
Customer evidence

Customer evidence should not automatically become training or reusable knowledge.

The production system should define:

data retention periods
delete and export procedures
customer approval boundaries for reuse
telemetry redaction rules

122. Governance

Governance should cover:

source provenance
card lifecycle
agent changes
decision history
customer data
access
trust assessments
production handoff

The production operating model should assign explicit owners for:

product ownership
card curation
agent policy
data stewardship
security and privacy approval
production operations
123. Card Lifecycle

Reusable cards should have:

DRAFT
REVIEW
PUBLISHED
DEPRECATED
RETIRED

Each card should have an owner, a review cadence, and a retirement path.

124. Card Quality Review

Cards should be reviewed for:

clarity
accuracy
usefulness
evidence
duplication
outdated technology
inappropriate assumptions
industry applicability

Review outcomes should be recorded and auditable.

125. Card Versioning

Changing a card should not silently change historical engagement records.

An engagement should reference:

Card:
AI-RETRIEVAL


Version:
1.3

Historical engagements remain reproducible.

126. Source Versioning

Likewise:

Source:
Microsoft Agent Guides


Version:
<commit/reference>

This makes the system auditable.

127. Method Versioning

Engagements should record:

Method:
AI Opportunity Engineering


Version:
3.0

This allows later comparison of methodology changes.

128. Industry Neutrality

The core method must remain applicable outside Microsoft and outside a single industry.

The system should therefore distinguish:

CORE METHOD
       +
DOMAIN PACK
       +
TECHNOLOGY ADAPTER
       +
INDUSTRY PACK
129. Example End-to-End Journey

Customer says:

"Our specialists spend too much time preparing cases."

The system does not immediately create an AI concept.

It captures:

Statement
   ↓
Evidence
   ↓
Workflow
   ↓
Problem

Then:

Problem:
Specialists spend significant time gathering context.

Opportunity:

Reduce case preparation effort.

Then alternatives:

Knowledge consolidation
AI retrieval
Workflow automation
Process redesign

Then evidence:

42 minute baseline
3 systems
2 participant confirmations

Then assumptions:

20+ minutes is spent searching.

Then experiment:

Measure 20 cases.

Then result:

Average search effort = 17 minutes.

Confidence changes.

The decision changes.

That decision becomes a pilot or is rejected.

This is the system working correctly.

130. What the System Must Never Become

It must not become:

An AI idea generator

Too shallow.

A card game

Too gimmicky.

A generic design-thinking workshop

Too broad.

A consulting slide generator

Too disconnected from engineering.

An autonomous decision maker

Too risky.

A product recommender disguised as discovery

Too technology-first.

A scoring spreadsheet

Too simplistic.

A giant knowledge-management system

Too broad.

131. What It Should Become

It should become:

A visual, evidence-backed opportunity engineering environment.

Where:

Cards
   ↓
represent
   ↓
Domain Objects
   ↓
connected by
   ↓
Opportunity Graph
   ↓
interpreted by
   ↓
Facilitator Agent
   ↓
evaluated by
   ↓
Decision Engine
   ↓
validated by
   ↓
Experiments
   ↓
measured through
   ↓
Outcomes
132. Phase 0 - Production Method Foundation

Build:

domain model
schemas
lifecycle
evidence model
decision model
card taxonomy
operating model

No application dependency.

133. Phase 1 - Production Core Platform

Build:

card library
card renderer
filters
search
pinning
comparison
basic linking
JSON persistence
audit trail
identity integration
role-based access

Use real engagement data.

134. Phase 2 - Assisted Opportunity Operations

Add:

graph-aware agent
evidence extraction
challenge
card matching
opportunity generation
experiment creation
decision support
abstain-on-low-evidence behaviour
135. Phase 3 - Collaborative and Asynchronous Experience

Add:

collaborative workspace
live graph
participant interactions
voting
facilitator controls
workshop state machine
asynchronous review flows
136. Phase 4 - Validation and Delivery Control

Add:

experiments
measurements
confidence updates
pilot tracking
outcome recording
architecture handoff
engineering handoff
137. Phase 5 - Governed Production Rollout

Add:

security
privacy
governance
production readiness
operational support
backup and recovery
138. Phase 6 - Portfolio Intelligence

Add:

cross-engagement analysis
pattern detection
outcome analytics
reusable opportunity intelligence
139. Phase 7 - Advanced Agentic Capabilities

Only after sufficient evidence:

specialist agents
autonomous research
automated evidence synthesis
opportunity monitoring
proactive validation recommendations
140. Build-vs-Buy Principle

The system should use existing capabilities wherever possible.

Do not build:

authentication
generic collaboration
generic chat
generic document storage
generic analytics

unless the product requires unique behaviour.

The unique investment should be in:

Opportunity Graph
Card Engine
methodology
evidence model
decision model
validation loop
facilitator intelligence
141. Core Technical Architecture

A production implementation should follow this structure:

                   ┌─────────────────┐
                   │   Web Client     │
                   │                 │
                   │ Card Engine      │
                   │ Workshop UX      │
                   │ Async Review UX  │
                   └────────┬────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │ Opportunity API │
                   └────────┬────────┘
                            │
             ┌──────────────┼──────────────┐
             ▼              ▼              ▼
       Opportunity       Card         Decision
          Graph          Service       Engine
             │              │              │
             └──────────────┼──────────────┘
                            ▼
                    ┌──────────────┐
                    │ Agent Layer  │
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
           Evidence     Matching     Validation
           Tools         Tools         Tools

The production architecture must also define:

identity and SSO integration
role-based access control
customer workspace or tenant isolation
audit logging
observability boundaries
retention and deletion controls
backup and recovery
environment separation
event-processing adapter for graph-change detection and governed downstream workflow triggers (Drasi is explicitly permitted as a candidate; the canonical Opportunity Graph remains the source of truth and the adapter boundary is non-authoritative)

The graph remains a domain model first.

142. Graph Storage Evolution

Recommended progression:

JSON
 ↓
Document store
 ↓
Relational + relationships
 ↓
Graph database if justified

Do not start with a graph database simply because the conceptual model is a graph.

The graph is a domain model first.

143. API Principle

The API should operate on domain objects.

Example:

GET /engagements/{id}


GET /opportunities/{id}


GET /opportunities/{id}/evidence


GET /opportunities/{id}/concepts


GET /opportunities/{id}/experiments


POST /opportunities


POST /experiments


POST /decisions


GET /cards


POST /cards/match


POST /opportunities/compare
144. Event Model

The production architecture MUST support an event-processing adapter that emits the following events for graph-change detection, governed downstream triggers, and auditability. All triggers MUST be auditable, replay-safe, and non-authoritative relative to the canonical Opportunity Graph (the adapter boundary does not own the graph state):

OpportunityCreated
EvidenceAdded
EvidenceConflictDetected
ConceptCreated
AssumptionCreated
ExperimentStarted
ExperimentCompleted
ConfidenceChanged
DecisionChanged
PilotStarted
OutcomeRecorded

These events enable re-score, re-summarization, policy/readiness gating, and reviewer notification workflows. Drasi is explicitly permitted as a candidate event-processing adapter implementation.

145. Observability

The system should observe:

agent actions
recommendations
human overrides
card usage
graph changes
decision changes
validation outcomes
security-relevant events

Agent telemetry must not become customer surveillance.

Only collect what is needed.

Operational telemetry should be separated from reusable knowledge and handled according to retention and redaction rules.

146. Agent Evaluation

Evaluate:

Grounding

Did the agent use the correct evidence?

Factuality

Did it introduce unsupported claims?

Challenge

Did it identify important gaps?

Recommendation

Did suggested cards/interventions make sense?

Decision support

Did the recommendation improve decision quality?

Calibration

Did confidence correspond to evidence strength?

147. Recommendation Evaluation

A recommendation should be judged on:

Relevance
Evidence alignment
Constraint alignment
Trust alignment
Workflow alignment
Outcome alignment

Not simply whether the user clicked it.

148. Human-in-the-Loop Principle

The human should remain in control particularly when:

evidence conflicts
trust is high-risk
decisions affect customers
decisions affect employees
autonomy is high
financial commitments are involved
regulatory obligations exist
149. Product Principle: Make Uncertainty Visible

The system should deliberately surface:

Known
Unknown
Assumed
Disputed
Blocked
Validated

This is one of the strongest differentiators.

150. Product Principle: Make Decisions Reversible

Where possible:

Don't know
   ↓
Cheap experiment
   ↓
Evidence
   ↓
Decision

rather than:

Don't know
   ↓
Build expensive system
   ↓
Hope
151. Product Principle: Build Only When Justified

The system itself should enforce:

Discovery does not equal permission to build.

An opportunity needs sufficient evidence and an appropriate decision before engineering investment.

152. Definition of Done - Opportunity

An opportunity is sufficiently formed when:

problem defined
user identified
workflow identified
evidence attached
outcome defined
value assessed
confidence assessed
alternatives considered
owner identified
153. Definition of Done - Pilot

A pilot candidate requires:

measurable baseline
target
owner
scope
concept
trust posture
autonomy
dependencies
validation plan
success criteria
154. Definition of Done - Outcome

An outcome requires:

baseline
target
actual
measurement method
measurement period
interpretation
recommendation
155. Definition of Done - Card

A reusable card requires:

unique ID
type
title
description
provenance
tags
source/version
owner
review date
lifecycle status
156. Definition of Done - Agent

The Facilitator Agent is ready when:

it can operate from graph state
it preserves evidence provenance
it distinguishes assumptions
it detects contradictions
it recommends relevant cards
it explains recommendations
it respects human authority
it works without hidden context
it passes adversarial evaluation
157. Definition of Done - Workshop

A workshop is successful when:

participants understand the problem
evidence is visible
alternatives were considered
opportunities are prioritised
uncertainty is explicit
decisions are recorded
owners are identified
next actions exist

The number of ideas generated is not a success criterion.

158. Definition of Done - System

The system is successful when it consistently helps teams:

Understand
    ↓
Frame
    ↓
Compare
    ↓
Validate
    ↓
Decide
    ↓
Deliver
    ↓
Measure

with less ambiguity and less rediscovery.

159. Strategic Differentiators

The strongest differentiators are:

Opportunity Graph
Card Engine
Evidence provenance
Visual comparison
Explainable matching
Value × Confidence
Trust and readiness gates
Assumption-to-experiment loop
Human-controlled Facilitator Agent
Delivery handoff
Outcome tracking
Technology-neutral core
160. Final Product Model

The final system should be understood as:

                         AI OPPORTUNITY
                         ENGINEERING
                             SYSTEM
                               │
       ┌───────────────────────┼────────────────────────┐
       │                       │                        │
       ▼                       ▼                        ▼
   CARD ENGINE          OPPORTUNITY GRAPH        FACILITATOR AGENT
       │                       │                        │
       │                       │                        │
 Browse / Match          Evidence                  Discover
 Compare / Connect       Workflow                  Challenge
 Promote / Filter        Problem                   Recommend
       │                 Opportunity                Explain
       │                 Concept                    Validate
       │                 Experiment                 Handoff
       │                 Decision
       └───────────────────────┬────────────────────────┘
                               ▼
                       DECISION ENGINE
                               │
                 ┌─────────────┼─────────────┐
                 ▼             ▼             ▼
               PILOT        VALIDATE       PARK
                 │             │
                 ▼             ▼
             MEASURE       EXPERIMENT
                 │             │
                 └──────┬──────┘
                        ▼
                     OUTCOME
                        │
               ┌────────┼────────┐
               ▼        ▼        ▼
             SCALE    REDESIGN   STOP
161. Final Recommendation

The v3 architecture should not be implemented as a workshop application first.

The correct sequence is:

1. Domain model
        ↓
2. Card model
        ↓
3. Card library
        ↓
4. Opportunity Graph
        ↓
5. Facilitator Agent
        ↓
6. Workshop experience
        ↓
7. Validation loop
        ↓
8. Pilot tracking
        ↓
9. Outcome tracking
        ↓
10. Portfolio intelligence

# 162. Visual Product Design Direction

The AI Opportunity Engineering System SHOULD borrow the strongest interaction and visual-design principles from the DJTools Azure VM SKU Locator, while maintaining an original visual language appropriate to opportunity engineering.

Reference implementation:

https://github.com/DarrenJohns/djtools-azure-vm-sku-locator

The intent is not to reproduce the application's UI.

The intent is to adopt proven interaction patterns for:

- discovery
- filtering
- matching
- comparison
- pinning
- progressive disclosure
- information-dense cards
- visual status indicators
- detail views
- contextual actions

The system should feel like a professional engineering tool rather than a generic AI chat application.


# 163. Core Navigation Model

The primary navigation should be organised around user intent.

Recommended top-level experiences:

    BROWSE
    FIND A MATCH
    PIN & COMPARE
    OPPORTUNITIES
    WORKFLOWS
    VALIDATION
    DECISIONS

The exact navigation labels may evolve, but the intent-based structure should remain.

The user should be able to enter the system through either:

    "I want to explore."

or:

    "I have a problem and want to find something that fits."

or:

    "I already have candidates and want to compare them."


# 164. Browse Experience

Browse is the primary exploratory experience.

Users should be able to browse:

- opportunities
- problems
- interventions
- concepts
- workflows
- experiments
- trust patterns
- readiness patterns
- KPI patterns

The Browse experience should use cards as the default representation.

Example:

    ┌──────────────────────────────────────────────┐
    │ AI-ASSISTED RESEARCH                        │
    │                                              │
    │ Reduce time spent gathering case context.   │
    │                                              │
    │ VALUE       CONFIDENCE      TRUST            │
    │ HIGH        MEDIUM          MEDIUM           │
    │                                              │
    │ Research • Knowledge • Human review          │
    │                                              │
    │ Evidence ●●●                                 │
    │                                              │
    │ [ View ] [ Pin ]                             │
    └──────────────────────────────────────────────┘


# 165. Find a Match Experience

"Find a Match" should be a first-class product capability.

The user describes a requirement such as:

    "Find opportunities where specialists spend
     significant time researching information."

The system converts the request into matching dimensions:

    Workflow
    Problem
    Desired outcome
    User
    Constraints
    Trust
    Readiness
    Autonomy

The result is a ranked set of candidate cards.

The system MUST explain the match.

Example:

    MATCH: STRONG

    ✓ Same workflow pattern
    ✓ Same desired outcome
    ✓ Human approval compatible
    ✓ Suitable autonomy

    ⚠ Data readiness differs

    Why this matters:
    This pattern addresses the same core problem,
    but requires validation of the customer's data
    availability before proceeding.


# 166. Match Results

Match results should be displayed as cards rather than a raw result list.

Each result should expose:

    Match strength
    Why it matches
    Why it may not match
    Evidence basis
    Trust considerations
    Readiness considerations
    Recommended next action

The system MUST avoid presenting a match score as objective truth.

For example:

    GOOD

    Strong match
    4 of 5 dimensions align

rather than:

    BAD

    87.42% match


# 167. Pinning

Users should be able to pin cards.

Pinned cards form a temporary working set.

Example:

    PINNED
    ─────────────────────────────────────

    [Opportunity A]
    [Opportunity B]
    [Concept C]
    [Experiment D]

    [ Compare 4 ]

Pinning should be available throughout the system.

Pinning is a user-interface state and SHOULD NOT automatically create a domain relationship.


# 168. Comparison Workspace

The comparison workspace is a core interaction.

Users should be able to select multiple cards and compare them across consistent dimensions.

Example:

                          A              B              C

    Value                HIGH           HIGH           MEDIUM
    Confidence           HIGH           MEDIUM         LOW
    Evidence             STRONG         MODERATE       WEAK
    Trust                LOW            MEDIUM         HIGH
    Readiness             HIGH           MEDIUM         LOW
    Complexity             LOW            MEDIUM        HIGH
    Time to value          FAST           MEDIUM        SLOW
    Autonomy               A2             A3            A4

    Recommended decision  PILOT          VALIDATE      RESEARCH


# 169. Comparison Must Be Contextual

Comparison dimensions MUST adapt to the object being compared.

For opportunities:

    Value
    Confidence
    Evidence
    Trust
    Readiness
    KPI
    Owner

For concepts:

    Value
    Complexity
    Trust
    Autonomy
    Dependencies
    Time to value

For experiments:

    Cost
    Time
    Evidence gain
    Risk
    Decision impact

The system should not force every object into the same generic scorecard.


# 170. Card Grid

The default browse experience should use a responsive card grid.

Example:

    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
    │ OPPORTUNITY  │  │ OPPORTUNITY  │  │ OPPORTUNITY  │
    │              │  │              │  │              │
    │ Research     │  │ Case triage  │  │ Knowledge    │
    │ Assistant    │  │              │  │ discovery    │
    │              │  │              │  │              │
    │ HIGH VALUE   │  │ HIGH VALUE   │  │ MEDIUM       │
    │ MED CONF.    │  │ HIGH CONF.   │  │ CONFIDENCE   │
    │              │  │              │  │              │
    │ [View] [Pin] │  │ [View] [Pin] │  │ [View] [Pin] │
    └──────────────┘  └──────────────┘  └──────────────┘

The number of columns should adapt to viewport size.


# 171. Card Visual Hierarchy

Every card should have a consistent hierarchy:

    1. Type
    2. Title
    3. One-line description
    4. Primary decision signals
    5. Supporting metadata
    6. Evidence / trust / readiness
    7. Primary action

The title and primary decision signals should be visually dominant.

Metadata should remain subordinate.


# 172. Card Header

The card header SHOULD contain:

    Type indicator
    Icon
    Title
    Optional lifecycle state

Example:

    ┌─────────────────────────────────────┐
    │ ✦ OPPORTUNITY              VALIDATE │
    │                                     │
    │ Reduce case preparation effort      │
    └─────────────────────────────────────┘

The visual treatment should immediately communicate what kind of object the user is viewing.


# 173. Card Signal Row

Cards should use compact signal components.

Example:

    VALUE          CONFIDENCE       TRUST
    HIGH           MEDIUM           MEDIUM

or:

    ● High         ● Medium         ⚠ Medium

Signals should be consistent throughout the application.


# 174. Evidence Visualization

Evidence should be visible without opening the card.

Recommended pattern:

    EVIDENCE
    ●●● Strong

or:

    EVIDENCE
    3 observed
    2 measured
    1 assumption

Hovering or selecting the indicator can expose details.

The user should never have to open a card simply to discover that the underlying opportunity has no evidence.


# 175. Trust Visualization

Trust should be represented using compact, understandable indicators.

Example:

    TRUST
    ✓ Human review
    ✓ Auditability
    ⚠ Sensitive data

Trust indicators should link to the detailed Trust Profile.


# 176. Readiness Visualization

Readiness should similarly expose blockers.

Example:

    READINESS
    ✓ Owner
    ✓ KPI
    ✓ Data
    ⚠ Integration
    ✗ Governance

This allows a user to understand immediately why a high-value opportunity may not be pilot-ready.


# 177. Lifecycle Visualization

Lifecycle should be visually obvious.

Recommended states:

    DISCOVERY
    FRAMED
    SHORTLISTED
    VALIDATING
    DECIDED
    PILOT
    MEASURING
    SCALED
    PARKED
    STOPPED

Lifecycle state should be represented by both text and visual treatment.

Colour MUST NOT be the sole mechanism for communicating state.


# 178. Card Actions

Cards should expose a small number of contextual actions.

Typical actions:

    View
    Pin
    Compare
    Connect
    Promote
    Validate
    Reject

The action set should depend on lifecycle state.

A card in DISCOVERY should not expose:

    "Deploy"

A card in PILOT should not expose:

    "Create opportunity"

unless there is a legitimate reason.


# 179. Progressive Disclosure

Cards should have three presentation modes:

    COMPACT
    STANDARD
    EXPANDED

COMPACT:

    Type
    Title
    Primary signal
    Lifecycle

STANDARD:

    Description
    Value
    Confidence
    Evidence
    Trust
    Readiness
    Actions

EXPANDED:

    Full evidence
    Workflow
    Assumptions
    Concepts
    Decisions
    Provenance
    Related cards


# 180. Detail View

Selecting a card should open a detail experience rather than navigating away from the user's working context wherever practical.

The detail view should preserve the surrounding browse/compare state.

Recommended layout:

    ┌───────────────────────────────────────────────────┐
    │ OPPORTUNITY                              [Close]   │
    │                                                   │
    │ Reduce case preparation effort                    │
    │                                                   │
    │ VALUE      CONFIDENCE     TRUST      READINESS    │
    │ HIGH       MEDIUM         MEDIUM     HIGH         │
    │                                                   │
    │ ────────────────────────────────────────────────  │
    │                                                   │
    │ WHY                                               │
    │ Problem and desired outcome                       │
    │                                                   │
    │ EVIDENCE                                          │
    │ ●●● Strong                                        │
    │                                                   │
    │ CONCEPTS                                          │
    │ [AI Retrieval] [Knowledge] [Automation]          │
    │                                                   │
    │ ASSUMPTIONS                                       │
    │ [2 unresolved]                                    │
    │                                                   │
    │ DECISION                                          │
    │ VALIDATE                                          │
    │                                                   │
    │ [Create experiment]                              │
    └───────────────────────────────────────────────────┘


# 181. Filtering

Filtering should be prominent and fast.

Supported filters include:

    Type
    Industry
    Workflow
    Value
    Confidence
    Evidence
    Trust
    Readiness
    Lifecycle
    Autonomy
    Owner
    Technology
    Source

Filters should be composable.

Example:

    Opportunities
    WHERE
      Value = High
      AND Confidence >= Medium
      AND Trust != Blocked
      AND Lifecycle = Shortlisted


# 182. Search

Search should support both:

    lexical search

and:

    semantic search

Users should be able to search:

    "research workload"

as well as:

    "opportunities where employees spend too much
     time finding information"

The system should make the distinction between exact matches and semantic matches visible where useful.


# 183. Sort Options

Cards should support useful sort options.

Examples:

    Relevance
    Value
    Confidence
    Evidence strength
    Readiness
    Time to value
    Recently updated
    Recently validated

The default sort should depend on the user's current task.


# 184. Empty States

Empty states should guide the user.

Bad:

    No results.

Better:

    No high-value opportunities match these filters.

    Try:
    • Lowering the confidence requirement
    • Removing the trust filter
    • Searching another workflow


# 185. Loading States

The application should avoid blank screens while agent or matching operations run.

Use meaningful progress states.

Example:

    ANALYSING

    ✓ Reading opportunity
    ✓ Checking evidence
    ● Comparing workflow patterns
    ○ Evaluating trust constraints


# 186. Recommendation Cards

Agent recommendations should use the same visual language as ordinary cards.

Example:

    ┌──────────────────────────────────────────────┐
    │ ✦ RECOMMENDATION                             │
    │                                              │
    │ Validate search effort before building       │
    │ an AI retrieval solution.                    │
    │                                              │
    │ WHY                                          │
    │ The current evidence is based on two        │
    │ participant estimates.                       │
    │                                              │
    │ [Create experiment] [Dismiss]               │
    └──────────────────────────────────────────────┘

The agent should not dominate the interface.


# 187. Agent as a Sidecar

The preferred interaction model is:

    PRIMARY SURFACE
        Cards / Graph / Comparison

    SECONDARY SURFACE
        Facilitator Agent

The agent should operate as a sidecar rather than replacing the main product experience with a chat window.

The user should be able to reason visually without talking to the agent.


# 188. Visual Graph View

A graph view SHOULD be available as an alternative to the card view.

Example:

    Evidence
       │
       ▼
    Problem
       │
       ▼
    Opportunity
      / \
     /   \
    ▼     ▼
 Concept A  Concept B
     │
     ▼
 Experiment
     │
     ▼
 Decision

The graph is useful for understanding relationships.

It should not replace the card interface.


# 189. Graph + Cards

Selecting a graph node should open its corresponding card.

Conversely:

    Card → Show in graph

This creates two complementary interaction modes:

    Cards = understand and compare

    Graph = understand relationships


# 190. Visual Relationship Types

Relationships should be explicitly typed.

Examples:

    EVIDENCES
    DERIVED_FROM
    OCCURS_IN
    ADDRESSES
    ALTERNATIVE_TO
    DEPENDS_ON
    CONTRADICTS
    VALIDATES
    LEADS_TO
    BLOCKS
    MEASURES
    OWNED_BY

The UI should visually distinguish important relationship types.


# 191. Visual Language

The system should develop its own visual language inspired by engineering tools.

Characteristics:

    Dense but readable
    Professional
    Data-oriented
    Calm
    Structured
    High signal
    Minimal decoration

Avoid:

    Excessive gradients
    Floating AI sparkles
    Generic chatbot aesthetics
    Gamified scoring
    Decorative 3D elements
    Excessive animation


# 192. Information Density

The VM SKU Locator demonstrates a useful principle:

    Put meaningful technical information
    directly into the browsing experience.

The Opportunity Engineering System should apply the same principle.

Users should not need to open five dialogs to answer:

    Is this valuable?

    Is it credible?

    Is it trusted?

    Is it ready?

    What happens next?


# 193. "Technical Tool" UX

The application should feel closer to:

    Azure Portal tooling
    Architecture tooling
    Developer tooling
    Engineering analysis tools

than:

    generic SaaS dashboard
    AI chatbot
    marketing website

This is intentional.

The target user is making engineering and business decisions.


# 194. Visual Comparison With Source Pattern

The following concepts are explicitly borrowed as interaction patterns:

    Browse
    Find a Match
    Pin & Compare
    Information-rich cards
    Detail views
    Filters
    Structured metadata
    Progressive disclosure
    Visual status indicators

The following are NOT copied:

    Azure VM-specific visual language
    VM SKU terminology
    Exact component styling
    Exact layout
    Exact colour palette
    Exact icons
    Exact typography
    Source application's branding


# 195. Card Design System

The Card Engine SHOULD have its own design-system specification.

Define reusable components:

    Card
    CardHeader
    CardSignal
    CardBadge
    EvidenceIndicator
    TrustIndicator
    ReadinessIndicator
    LifecycleBadge
    MatchIndicator
    PinButton
    CompareButton
    RelationshipIndicator
    CardAction
    DetailPanel
    FilterBar
    ComparisonPanel

These components should be reused across card types.


# 196. Card Schema and Presentation Schema

The domain object and visual representation should remain separate.

Example:

    DOMAIN

    Opportunity
      id
      problem
      evidence
      value
      confidence
      trust
      readiness


    PRESENTATION

    OpportunityCard
      layout
      density
      visibleSignals
      badges
      actions
      visualState

This allows the same opportunity to be rendered differently for:

    Workshop
    Desktop
    Mobile
    Executive
    Print
    PowerPoint


# 197. Visual State Must Be Derived

The UI should derive visual state from domain state.

Example:

    confidence = LOW
    evidence = WEAK
    trust = BLOCKED
    readiness = LOW

should automatically produce an appropriate visual representation.

The user should not manually maintain visual badges separately from the domain model.


# 198. No Decorative Metrics

Every number shown on a card must answer:

    What decision does this number help make?

If it does not help a decision, it should not be displayed.

Avoid dashboard-style metric inflation.


# 199. Visual Priority

The visual hierarchy should favour:

    Decision relevance
          ↓
    Evidence
          ↓
    Constraints
          ↓
    Metadata

rather than:

    Marketing value
          ↓
    AI novelty
          ↓
    Technology
          ↓
    Evidence


# 200. Final UX Principle

The strongest visual lesson to borrow from the VM SKU Locator is not "use nice cards."

It is:

    Make a complex technical decision space
    browseable, comparable and understandable
    through structured visual objects.

That principle should become a formal design objective of the AI Opportunity Engineering System.

The most important architectural decision is the Opportunity Graph.

The most important UX decision is the Card Engine.

The most important AI decision is the graph-aware Facilitator Agent.

The most important methodological decision is evidence before enthusiasm.

The most important decision mechanism is Value × Confidence with Trust and Readiness gates.

The most important engineering principle is validate before building.

And the most important product principle is:

The card is not the product. The card is the interface through which people reason about the opportunity graph.

That distinction gives the system room to evolve from a highly effective facilitated workshop into a Forward Deployed Engineering platform for AI opportunity discovery, validation, delivery, and measurable outcomes.