import { EvidenceType, type EvidenceTypeValue } from '../api/contracts'

export type EvidenceDraft = {
  readonly type: EvidenceTypeValue
  readonly statement: string
  readonly sourceReference: string
  readonly participantReference: string
  readonly interpretation: string
  readonly confidence: string
}

export const emptyEvidenceDraft: EvidenceDraft = {
  type: EvidenceType.Observed,
  statement: '',
  sourceReference: '',
  participantReference: '',
  interpretation: '',
  confidence: '0.8',
}
