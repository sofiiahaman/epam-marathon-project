export type ParticipantType = 'Owner' | 'Guest';

export interface ParticipantDish {
  id: string;
  name: string;
  price: number;
}

export interface ParticipantItem {
  dish: ParticipantDish;
  quantity: number;
}

export interface Participant {
  /** Guest session id. `null` when the participant has no session yet (e.g. an owner who has not started one). */
  id: string | null;
  type: ParticipantType;
  name: string;
  items: ParticipantItem[];
  total: number;
  isSubmitted: boolean;
}

/** Snapshot of everyone in a group pre-order: returned by the list and delete-session endpoints. */
export interface ParticipantsSnapshot {
  participants: Participant[];
  total: number;
}

/** One line of the consolidated order: the same dish from all submitted sessions merged by `dishId`. */
export interface ConsolidatedItem {
  dishId: string;
  quantity: number;
}

export interface OwnerGuestSession {
  guestSessionId: string;
}

export interface SubmitGroupRequest {
  reservationId: string;
}
