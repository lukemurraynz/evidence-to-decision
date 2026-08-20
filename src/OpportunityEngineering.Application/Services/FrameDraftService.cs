using Microsoft.Agents.AI.Workflows;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>Interactive, synchronous counterpart to the async recommendation flow. A
/// facilitator wants a starting draft while looking at the Frame stage, not to poll an
/// operation. Carries the same guardrail policy checks and audit trail every model call in
/// this codebase goes through. Runs the draft agent and citation-critique agent as a two-node
/// <c>Microsoft.Agents.AI.Workflows</c> graph rather than one bare agent call: draft feeds
/// critique automatically, so a facilitator never sees a candidate whose citations haven't
/// already been checked.</summary>
public sealed class FrameDraftService(
    IOpportunityGraphStore graphStore,
    IFrameDraftAgent draftAgent,
    IFrameCritiqueAgent critiqueAgent,
    IAgentPolicyEvaluator policyEvaluator,
    IAppendOnlyAuditSink auditSink,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    public async Task<FrameDraftResult> DraftAsync(
        ActorContext actor,
        string engagementId,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);

        var graph = await graphStore.GetAsync(actor.WorkspaceId, engagementId, cancellationToken)
            ?? throw new DomainException("engagement.not_found", "Engagement was not found.");

        var modelCallPolicy = await policyEvaluator.EvaluateAsync(
            actor,
            "frame_draft.model_call",
            "foundry-frame-draft",
            cancellationToken);
        await AuditAsync("frame_draft.model_call", modelCallPolicy, "foundry-frame-draft");
        if (!modelCallPolicy.Permitted)
        {
            throw new DomainException(
                "policy.model_call_denied",
                "Policy denied or escalated the model call.");
        }

        var result = await RunWorkflowAsync();

        var outputPolicy = await policyEvaluator.EvaluateAsync(
            actor,
            "frame_draft.output",
            null,
            cancellationToken);
        await AuditAsync("frame_draft.output", outputPolicy, null);
        // A ternary here would push the return value into the false-branch of a
        // throw-expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
        if (!outputPolicy.Permitted)
        {
            throw new DomainException(
                "policy.output_denied",
                "Policy denied or escalated the draft output.");
        }
#pragma warning restore IDE0046

        return result;

        async Task AuditAsync(string evaluationPoint, PolicyDecision decision, string? toolName)
        {
            await auditSink.AppendAsync(
                new PolicyAuditRecord(
                    identifiers.Create(),
                    actor.WorkspaceId,
                    actor.ActorId,
                    decision.PolicyVersion,
                    evaluationPoint,
                    decision.Verdict,
                    decision.Reason,
                    timeProvider.GetUtcNow(),
                    actor.CorrelationId,
                    toolName),
                cancellationToken);
        }

        async Task<FrameDraftResult> RunWorkflowAsync()
        {
            var draftBinding = new FunctionExecutor<string, FrameDraftResult>(
                "draft",
                async (_, _, ct) => await draftAgent.DraftAsync(graph, actor, ct))
                .BindExecutor();
            var critiqueBinding = new FunctionExecutor<FrameDraftResult, FrameDraftResult>(
                "critique",
                async (draft, _, ct) => await CritiqueAllAsync(draft, ct))
                .BindExecutor();

            var workflow = new WorkflowBuilder(draftBinding)
                .AddEdge(draftBinding, critiqueBinding)
                .WithOutputFrom(critiqueBinding)
                .Build();

            // InProcessExecution.RunAsync's snapshot isn't reliable here: it can return before
            // the critique node's real (network-bound) work finishes, leaving OutgoingEvents
            // empty. WatchStreamAsync drives the run to actual completion instead.
            await using var run = await InProcessExecution.RunStreamingAsync(
                workflow, "start", cancellationToken: cancellationToken);
            await foreach (var workflowEvent in run.WatchStreamAsync(cancellationToken))
            {
                if (workflowEvent is WorkflowOutputEvent outputEvent)
                {
                    return outputEvent.As<FrameDraftResult>()
                        ?? throw new DomainException(
                            "frame_draft.invalid_output",
                            "The frame draft workflow produced no output.");
                }
            }

            throw new DomainException(
                "frame_draft.invalid_output",
                "The frame draft workflow ended without producing output.");
        }

        // Skips the critique model call entirely when nothing was cited. An empty draft or a
        // draft whose candidates cite no evidence has nothing for a citation critique to check.
        async Task<FrameDraftResult> CritiqueAllAsync(FrameDraftResult draft, CancellationToken ct)
        {
            if (!draft.Candidates.Any(candidate => candidate.Problem.EvidenceReferences.Count > 0))
            {
                return draft;
            }

            var critiqueCallPolicy = await policyEvaluator.EvaluateAsync(
                actor, "frame_critique.model_call", "foundry-frame-critique", ct);
            await AuditAsync("frame_critique.model_call", critiqueCallPolicy, "foundry-frame-critique");
            if (!critiqueCallPolicy.Permitted)
            {
                throw new DomainException(
                    "policy.model_call_denied",
                    "Policy denied or escalated the critique call.");
            }

            var critiqued = await Task.WhenAll(draft.Candidates.Select(async candidate =>
                candidate.Problem.EvidenceReferences.Count == 0
                    ? candidate
                    : candidate with { CitationConcerns = await critiqueAgent.CritiqueAsync(graph, candidate, actor, ct) }));

            var critiqueOutputPolicy = await policyEvaluator.EvaluateAsync(
                actor, "frame_critique.output", null, ct);
            await AuditAsync("frame_critique.output", critiqueOutputPolicy, null);
            // A ternary here would push the return value into the false-branch of a
            // throw-expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
            if (!critiqueOutputPolicy.Permitted)
            {
                throw new DomainException(
                    "policy.output_denied",
                    "Policy denied or escalated the critique output.");
            }
#pragma warning restore IDE0046

            return draft with { Candidates = critiqued };
        }
    }

    private static void RequireFacilitatorMutation(ActorContext actor)
    {
        if (!actor.Has(ApplicationRole.Facilitator))
        {
            throw new DomainException(
                "authorization.canonical_mutation_denied",
                "Reviewers cannot request a frame draft.");
        }
    }
}
