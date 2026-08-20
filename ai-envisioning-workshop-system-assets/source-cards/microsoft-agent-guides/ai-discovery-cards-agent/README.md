# AI Discovery Cards Source Pack

## Purpose

This folder documents the canonical v1 source material adopted from `microsoft-partner-solutions-ai/agent-guides`, specifically the `ai-discovery-cards-agent` package.

The intent is not to replace the original files. It is to record how they should be used inside the AI Envisioning Workshop System.

## Source repository

- Repository: `microsoft-partner-solutions-ai/agent-guides`
- Package: `ai-discovery-cards-agent`

## Confirmed source assets

- `README.md`
- `system-instructions.md`
- `knowledge-sources.md`
- `suggested-prompts.md`
- `uploaded-files/cards.txt`
- `uploaded-files/AI Discovery Cards Workshop TTT_Partners_FY26.pptx`
- `uploaded-files/AI Discovery Cards Workshop FAQ.docx`
- `uploaded-files/ROTH AI Use Case and Tech Patterns.pptx`
- `uploaded-files/FY26_Partner_Execution_Guide_Oct 2025.pdf`

## Source-of-truth hierarchy

1. `cards.txt`
   - primary structured content source for categories, card names, example use cases, and mapped Azure/Microsoft services
2. `system-instructions.md`
   - primary operating guide for how the workshop agent should behave
3. `suggested-prompts.md`
   - primary interaction starter pack for real workshop flow and follow-up tasks
4. `README.md`
   - confirms the package purpose: AI Discovery Card workshop delivery, use-case ideation, and prioritization
5. `knowledge-sources.md`
   - identifies external reference sites and workshop assets expected to be loaded into the agent
6. PowerPoint / FAQ / PDF files in `uploaded-files/`
   - presentation, train-the-trainer, FAQ, and partner execution surfaces

## How this source pack should be used

- Use `cards.txt` to normalize the card library into machine-readable workshop metadata.
- Use `system-instructions.md` to define baseline facilitator-agent behaviors.
- Use `suggested-prompts.md` to seed workshop modes such as prep, persona mapping, card recommendation, cost discussion, and follow-up planning.
- Use PowerPoint and document assets as facilitation and enablement surfaces in live sessions.

## Important note

The original binary files were discovered and inventoried locally, but detailed slide-by-slide or page-by-page extraction was not available in this session. This pack records only what was verified from accessible text files and file inventory.
