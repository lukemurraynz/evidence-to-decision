# Normalized Card Schema

## Goal

Provide a clean, practical schema for converting `cards.txt` into reusable workshop data without losing traceability back to the original Microsoft-aligned source pack.

## Design principles

- Keep source traceability.
- Normalize category and card naming.
- Preserve workshop semantics, not just technical tags.
- Support compare, pin, shortlist, journey mapping, and recommendation flows.
- Allow future customer or industry overlays without mutating the canonical base set.

## Core entities

### Category

```json
{
  "id": "process-optimization",
  "displayName": "Process Optimization",
  "sourceName": "Process Optimization",
  "significance": "AI enhances efficiency, cost reduction, and resource allocation across industries.",
  "exampleUseCases": [
    "Predictive Maintenance: AI identifies equipment failures before they occur.",
    "Supply Chain Optimization: AI improves routing and logistics."
  ],
  "keyTechnologies": [
    "Microsoft Fabric",
    "Dynamics 365",
    "Azure Machine Learning"
  ],
  "source": {
    "repository": "microsoft-partner-solutions-ai/agent-guides",
    "package": "ai-discovery-cards-agent",
    "file": "uploaded-files/cards.txt"
  }
}
```

### Card

```json
{
  "id": "process-optimization-streamline-r-and-d",
  "displayName": "Streamline R&D",
  "sourceName": "Streamline R&D",
  "categoryId": "process-optimization",
  "description": "Optimize research and development processes through intelligent AI analysis.",
  "examples": [
    "Lab Efficiency - Automate and optimize laboratory processes for increased efficiency with AI.",
    "Product Success Prediction - Predict the success of new product developments based on historical data using AI."
  ],
  "microsoftServices": [
    "Azure Machine Learning",
    "Azure AI Foundry",
    "Microsoft Fabric",
    "Microsoft 365 Agents SDK"
  ],
  "workshopTags": [
    "opportunity",
    "process",
    "efficiency"
  ],
  "journeyStepFit": [],
  "source": {
    "repository": "microsoft-partner-solutions-ai/agent-guides",
    "package": "ai-discovery-cards-agent",
    "file": "uploaded-files/cards.txt"
  }
}
```

## Recommended normalization rules

### IDs

- Lowercase
- Hyphen-separated
- Stable across re-runs
- Derived from category and card name

### Category naming

Use a normalized canonical display name while preserving source spelling/casing.

Examples:

- `Navigation and Control` -> `navigation-and-control`
- `Data and predictive analytics` -> `data-and-predictive-analytics`
- `Decision making` -> `decision-making`
- `Content creation` -> `content-creation`

### Service naming

- Preserve source names initially.
- Do not prematurely deduplicate service aliases unless there is a strong reason.
- Later versions can add `normalizedServiceName` if required.

### Workshop tags

Derived tags should be additive, not destructive. Suggested tag families:

- `persona-relevance`
- `pattern-type`
- `trust-sensitivity`
- `industry-hints`
- `delivery-mode`

## Traceability requirement

Every normalized category and card must preserve:

- original source file
- original source name
- source package

This keeps the normalized layer auditable and aligned with the upstream Microsoft partner material.
