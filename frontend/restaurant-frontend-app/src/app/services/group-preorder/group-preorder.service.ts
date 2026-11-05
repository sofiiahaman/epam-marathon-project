import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ConsolidatedItem,
  OwnerGuestSession,
  ParticipantsSnapshot,
  SubmitGroupRequest,
} from '@app/models/group-preorder/group-preorder.model';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

/**
 * Owner-side API of the group pre-order.
 *
 * Unlike `GuestSessionsService`, none of these calls carry a guest session token: they are made by
 * the signed-in owner, so `tokenInterceptor` attaches the user's idToken (see the owner-endpoint
 * exception for `/guest/groups/{id}/sessions` in `token.interceptor.ts`). Do not add manual
 * `Authorization` headers here.
 */
@Injectable({ providedIn: 'root' })
export class GroupPreorderService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);

  getSessions(groupReservationId: string): Observable<ParticipantsSnapshot> {
    return this.http.get<ParticipantsSnapshot>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions`
    );
  }

  deleteSession(groupReservationId: string, sessionId: string): Observable<ParticipantsSnapshot> {
    return this.http.delete<ParticipantsSnapshot>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions/${encodeURIComponent(sessionId)}`
    );
  }

  getOwnerSessionId(reservationId: string): Observable<OwnerGuestSession> {
    return this.http.get<OwnerGuestSession>(
      `${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/guest-session-id`
    );
  }

  submitGroup(groupReservationId: string, reservationId: string): Observable<ConsolidatedItem[]> {
    const body: SubmitGroupRequest = { reservationId };
    return this.http.post<ConsolidatedItem[]>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/submit`,
      body
    );
  }
}
