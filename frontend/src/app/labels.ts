import {
  EngagementLifecycle,
  EvidenceModality,
  EvidenceType,
  OperationStatus,
  ValidationStatus,
  type EngagementLifecycleValue,
  type EvidenceTypeValue,
  type EvidenceModalityValue,
  type OperationStatusValue,
  type ValidationStatusValue,
} from '../api/contracts'

export function lifecycleLabel(value: EngagementLifecycleValue): string {
  const labels: Record<EngagementLifecycleValue, string> = {
    [EngagementLifecycle.Discovery]: 'Gathering evidence',
    [EngagementLifecycle.Validation]: 'Under review',
    [EngagementLifecycle.Pilot]: 'Pilot',
    [EngagementLifecycle.ProductionReadiness]: 'Ready for production',
    [EngagementLifecycle.Rejected]: 'Not proceeding',
    [EngagementLifecycle.Parked]: 'Paused',
  }
  return labels[value]
}

export function evidenceTypeLabel(value: EvidenceTypeValue): string {
  const labels: Record<EvidenceTypeValue, string> = {
    [EvidenceType.Observed]: 'Observed',
    [EvidenceType.Measured]: 'Measured',
    [EvidenceType.CustomerStatement]: 'Customer statement',
    [EvidenceType.External]: 'External',
    [EvidenceType.Interpretation]: 'Interpretation',
    [EvidenceType.Assumption]: 'Assumption',
    [EvidenceType.Hypothesis]: 'Hypothesis',
  }
  return labels[value]
}

export function validationLabel(value: ValidationStatusValue): string {
  const labels: Record<ValidationStatusValue, string> = {
    [ValidationStatus.Unvalidated]: 'Needs review',
    [ValidationStatus.NeedsCorrection]: 'Needs correction',
    [ValidationStatus.Validated]: 'Validated',
    [ValidationStatus.Rejected]: 'Not accepted',
  }
  return labels[value]
}

export function evidenceModalityLabel(value: EvidenceModalityValue): string {
  const labels: Record<EvidenceModalityValue, string> = {
    [EvidenceModality.Text]: 'Written note',
    [EvidenceModality.Voice]: 'Voice',
    [EvidenceModality.Transcript]: 'Transcript',
    [EvidenceModality.Document]: 'Document',
    [EvidenceModality.Image]: 'Image',
    [EvidenceModality.Mixed]: 'Mixed source',
  }
  return labels[value]
}

export function dateTimeLabel(value: string): string {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? 'Time unavailable'
    : new Intl.DateTimeFormat(undefined, {
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(date)
}

export function operationStatusLabel(value: OperationStatusValue): string {
  const labels: Record<OperationStatusValue, string> = {
    [OperationStatus.Queued]: 'Waiting to prepare the review brief',
    [OperationStatus.Running]: 'Preparing the review brief',
    [OperationStatus.Succeeded]: 'Review brief ready',
    [OperationStatus.Failed]: 'Review brief could not be prepared',
    [OperationStatus.Canceled]: 'Review brief preparation canceled',
  }
  return labels[value]
}
