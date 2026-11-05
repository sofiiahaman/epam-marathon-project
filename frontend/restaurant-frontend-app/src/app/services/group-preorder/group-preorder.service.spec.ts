import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  ConsolidatedItem,
  OwnerGuestSession,
  ParticipantsSnapshot,
} from '@app/models/group-preorder/group-preorder.model';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GroupPreorderService } from './group-preorder.service';

const BASE_URL = 'https://test.api';

describe('GroupPreorderService', () => {
  let service: GroupPreorderService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE_URL },
      ],
    });

    service = TestBed.inject(GroupPreorderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gets the group sessions snapshot', () => {
    const snapshot: ParticipantsSnapshot = { participants: [], total: 0 };
    let result: ParticipantsSnapshot | undefined;

    service.getSessions('group-1').subscribe((response) => (result = response));

    const request = httpMock.expectOne(`${BASE_URL}/guest/groups/group-1/sessions`);
    expect(request.request.method).toBe('GET');
    request.flush(snapshot);

    expect(result).toEqual(snapshot);
  });

  it('deletes a session and returns the updated snapshot', () => {
    const snapshot: ParticipantsSnapshot = { participants: [], total: 0 };
    let result: ParticipantsSnapshot | undefined;

    service.deleteSession('group-1', 'session-1').subscribe((response) => (result = response));

    const request = httpMock.expectOne(
      `${BASE_URL}/guest/groups/group-1/sessions/session-1`
    );
    expect(request.request.method).toBe('DELETE');
    request.flush(snapshot);

    expect(result).toEqual(snapshot);
  });

  it('gets the owner session id for a reservation', () => {
    const ownerSession: OwnerGuestSession = { guestSessionId: 'owner-session' };
    let result: OwnerGuestSession | undefined;

    service.getOwnerSessionId('reservation-1').subscribe((response) => (result = response));

    const request = httpMock.expectOne(
      `${BASE_URL}/reservations/reservation-1/guest-session-id`
    );
    expect(request.request.method).toBe('GET');
    request.flush(ownerSession);

    expect(result).toEqual(ownerSession);
  });

  it('submits the group reservation with the reservation id', () => {
    const consolidatedItems: ConsolidatedItem[] = [{ dishId: 'dish-1', quantity: 3 }];
    let result: ConsolidatedItem[] | undefined;

    service.submitGroup('group-1', 'reservation-1').subscribe((response) => (result = response));

    const request = httpMock.expectOne(`${BASE_URL}/group-reservations/group-1/submit`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ reservationId: 'reservation-1' });
    request.flush(consolidatedItems);

    expect(result).toEqual(consolidatedItems);
  });

  it('encodes path parameters', () => {
    service.deleteSession('group/a', 'session b').subscribe();

    const request = httpMock.expectOne(
      `${BASE_URL}/guest/groups/group%2Fa/sessions/session%20b`
    );
    expect(request.request.method).toBe('DELETE');
    request.flush({ participants: [], total: 0 });
  });

  it('propagates HTTP errors', () => {
    let status: number | undefined;

    service.submitGroup('group-1', 'reservation-1').subscribe({
      error: (error: { status: number }) => (status = error.status),
    });

    httpMock
      .expectOne(`${BASE_URL}/group-reservations/group-1/submit`)
      .flush({ message: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });

    expect(status).toBe(403);
  });
});
