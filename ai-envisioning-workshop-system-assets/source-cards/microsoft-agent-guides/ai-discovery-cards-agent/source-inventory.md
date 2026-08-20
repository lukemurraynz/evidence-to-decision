# Source Inventory

## Evidence captured from accessible text files

### Package purpose

From `README.md`:

- Agent name: `AI Discovery Card Agent`
- Description: supports AI Discovery Card workshop delivery with partners and customers
- Target use cases: workshop delivery, use-case ideation, prioritization

### Knowledge sources

From `knowledge-sources.md`:

- Websites listed:
  - Azure Pricing
  - Microsoft Learn Azure
  - Microsoft Use Case Explorer for AI Design Wins
  - Microsoft AI Solution Accelerators
- Uploaded files listed:
  - virtual AI Discovery Cards in txt format
  - workshop TTT PowerPoint
  - workshop FAQ
  - ROTH AI use case and tech patterns deck
  - FY26 partner execution guide PDF

### Agent operating model

From `system-instructions.md`:

- prepare discovery/interview questions before a workshop when context is missing
- extract business context, challenges, goals, participants, and roles from notes
- create persona and journey map
- recommend the most relevant 10 discovery cards
- attach cards to journey steps and explain why they fit
- suggest use cases and Microsoft services across Azure, M365, and Power Platform
- generate implementation guidance including estimated time, cost, and deployment considerations
- recommend follow-up engagement types such as PoC or architecture design session

### Prompt modes

From `suggested-prompts.md`:

- Prep Interview Call
- Persona & Journey Map
- AI Card Recommendation
- Implementation Cost
- Use Case & Accelerator
- Follow-up Engagement Planning

## Operational implications for the workshop system

- The card set is already part of a broader workshop operating method, not just a library of inspiration cards.
- Persona creation and journey mapping are upstream steps, not optional extras.
- Card recommendation is meant to be constrained to the top 10 most relevant cards.
- Solution recommendation and follow-up planning are built into the intended workflow.
- The system should preserve both the structured content layer and the facilitation/training surface.
