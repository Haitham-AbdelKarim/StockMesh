export const VERTICAL_CATEGORIES = [
  'Gaming',
  'Sports',
  'Pharmacy',
  'Electronics',
  'Fashion',
  'Grocery',
  'HomeImprovement',
  'Books',
  'Other',
] as const;
export type VerticalCategory = (typeof VERTICAL_CATEGORIES)[number];

export const MOVEMENT_TYPES = ['Sale', 'NetworkTransferOut', 'NetworkTransferIn', 'Restock'] as const;
export type MovementType = (typeof MOVEMENT_TYPES)[number];

export const RESERVATION_STATUSES = ['Pending', 'Accepted', 'Success', 'Cancelled'] as const;
export type ReservationStatus = (typeof RESERVATION_STATUSES)[number];

export const RECOMMENDED_ACTIONS = [
  'Hold',
  'Reorder',
  'Share',
  'UrgentShare',
  'MarketOpportunity',
] as const;

export const ROLES = ['Owner', 'Staff'] as const;
export type StoreRole = (typeof ROLES)[number];