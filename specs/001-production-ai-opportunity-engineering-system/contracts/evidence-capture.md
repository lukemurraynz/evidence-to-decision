# Contract: Evidence Capture

## Purpose

Capture attributable, modality-aware evidence without promoting extraction or interpretation to fact automatically.

## Input fields

- `engagementId`
- `type`: observed | measured | customer_statement | external | interpretation | assumption | hypothesis
- `statement` and optional `interpretation`
- `sourceReference`, optional `participantReference`, `capturedAt`
- `modality`: text | voice | transcript | document | image | mixed
- `confidence`, `validationStatus`
- optional multimodal asset and transcript segment references

## Rules

- The service must enforce workspace authorization before accepting the record.
- Original wording and source metadata are immutable after capture; corrections create an auditable revision.
- Transcript segments require speaker attribution, timestamps, extraction confidence, and human-correction status.
- Evidence conflicts are linked records and never silently averaged.
- Unvalidated or low-confidence extraction cannot satisfy a consequential evidence gate.
