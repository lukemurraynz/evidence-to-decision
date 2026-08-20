import type {
  Engagement,
  OpportunityReview,
} from '../api/contracts'

const trustProfile = {
  privacyApproved: true,
  securityApproved: true,
  governanceApproved: true,
  humanOversightDefined: true,
  dataSensitivity: 'Internal',
  auditability: 'Event history retained',
  modelRisk: 'Reviewed',
  operationalRisk: 'Moderate',
} as const

const readinessProfile = {
  ownerDefined: true,
  kpiDefined: true,
  baselineDefined: true,
  targetDefined: true,
  dataReady: true,
  processStable: true,
  integrationReady: true,
  changeCapacityReady: false,
} as const

export function engagementFixture(
  options: { readonly withOpportunity?: boolean } = {},
): Engagement {
  return {
    id: 'engagement-1',
    workspaceId: 'workspace-1',
    methodVersion: '1.0',
    owner: 'Workshop owner',
    governanceOwner: 'Review owner',
    objectVersion: 5,
    lifecycleState: 0,
    objectives: ['Reduce avoidable manual rework'],
    participants: ['Operations lead'],
    workflows: [],
    problems: [],
    personas: [],
    journeyMaps: [],
    cardShortlist: [],
    ideationNotes: [],
    evidence: [],
    opportunities:
      options.withOpportunity === true
        ? [
            {
              id: 'opportunity-1',
              problemId: 'problem-1',
              workflowId: 'workflow-1',
              desiredOutcome: 'Reduce case preparation time',
              kpiReference: 'Preparation time',
              owner: 'Operations owner',
              valueProfile: 'Faster preparation with review retained',
              confidenceProfile: 'Supported by workshop evidence',
              trustProfile,
              readinessProfile,
              evidenceReferences: [],
              concepts: [],
              lifecycleState: 0,
              objectVersion: 1,
            },
          ]
        : [],
    decisions: [],
    blockers: [],
  }
}

export function reviewFixture(): OpportunityReview {
  return {
    opportunityId: 'opportunity-1',
    engagementId: 'engagement-1',
    value: 'Faster preparation with review retained',
    confidence: 'Supported by workshop evidence',
    trust: trustProfile,
    readiness: readinessProfile,
    owner: 'Operations owner',
    evidenceReferences: [],
    blockers: [],
    latestDecision: null,
    canonicalGraphVersion: 5,
  }
}
