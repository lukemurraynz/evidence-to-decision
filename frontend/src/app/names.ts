export const PRODUCT_NAME = 'Evidence to Decision'

export const PRIMARY_NAVIGATION = [
  { route: 'discover', href: '#/discover', label: 'Evidence' },
  { route: 'ideation', href: '#/ideation', label: 'Ideation' },
  { route: 'frame', href: '#/frame', label: 'Frame' },
  { route: 'journey-map', href: '#/journey-map', label: 'Journey map' },
  { route: 'discovery-cards', href: '#/discovery-cards', label: 'Discovery cards' },
  { route: 'board', href: '#/board', label: 'Board' },
  { route: 'cards', href: '#/cards', label: 'Cards' },
  { route: 'review', href: '#/review', label: 'Decision review' },
  { route: 'outcomes', href: '#/outcomes', label: 'Outcomes' },
  { route: 'handoff', href: '#/handoff', label: 'Delivery documents' },
] as const

export const ROLE_STARTS = [
  {
    title: 'Workshop facilitator',
    description: 'Capture source evidence and frame the opportunity.',
    href: '#/discover',
    action: 'Capture evidence',
  },
  {
    title: 'Decision reviewer',
    description: 'Evaluate readiness, record a decision, and name blockers.',
    href: '#/review',
    action: 'Record a decision',
  },
  {
    title: 'Executive',
    description: 'Read outcomes, confidence, and readiness at a glance.',
    href: '#/outcomes',
    action: 'Review outcomes',
  },
  {
    title: 'Delivery lead',
    description: 'Prepare approved work for planning and delivery.',
    href: '#/handoff',
    action: 'Create a delivery document',
  },
] as const

export const PAGE_NAMES = {
  home: PRODUCT_NAME,
  discover: 'Evidence',
  ideation: 'Ideation',
  'discovery-cards': 'Discovery cards',
  board: 'Board',
  'journey-map': 'Journey map',
  frame: 'Frame',
  cards: 'Cards',
  review: 'Decision review',
  outcomes: 'Outcomes',
  handoff: 'Delivery documents',
  progress: 'Review brief status',
  join: 'Join the workshop',
  'not-found': 'Page not found',
} as const

export const ROLE_BY_ROUTE = {
  discover: 'Workshop facilitator',
  ideation: 'Workshop facilitator',
  'discovery-cards': 'Workshop facilitator',
  board: 'Workshop facilitator',
  'journey-map': 'Workshop facilitator',
  frame: 'Workshop facilitator',
  cards: 'Workshop facilitator',
  review: 'Decision reviewer',
  outcomes: 'Executive',
  handoff: 'Delivery lead',
  progress: 'Decision reviewer',
} as const
