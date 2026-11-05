import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '../services/toast/toast.service';
import { AuthenticationService } from './services/authentication.service';
import { SKIP_ERROR_TOAST } from './skip-error-toast.context';
import { tokenInterceptor } from './token.interceptor';

const URL = 'https://test.api/reservations';

describe('tokenInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toastService: jasmine.SpyObj<ToastService>;
  let authService: jasmine.SpyObj<AuthenticationService>;

  beforeEach(() => {
    toastService = jasmine.createSpyObj<ToastService>('ToastService', ['error']);
    authService = jasmine.createSpyObj<AuthenticationService>('AuthenticationService', [
      'getTokens',
    ]);
    authService.getTokens.and.returnValue(null);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([tokenInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toastService },
        { provide: AuthenticationService, useValue: authService },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows the server message in a toast for a 400 response', () => {
    http.post(URL, {}).subscribe({ error: () => undefined });

    httpMock
      .expectOne(URL)
      .flush({ message: 'Overlaps' }, { status: 400, statusText: 'Bad Request' });

    expect(toastService.error).toHaveBeenCalledWith('Overlaps', 'Error');
  });

  it('does not show a toast but still rethrows when SKIP_ERROR_TOAST is set', () => {
    let receivedStatus: number | undefined;
    http
      .post(URL, {}, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
      .subscribe({ error: (error) => (receivedStatus = error.status) });

    httpMock
      .expectOne(URL)
      .flush({ message: 'Overlaps' }, { status: 400, statusText: 'Bad Request' });

    expect(toastService.error).not.toHaveBeenCalled();
    expect(receivedStatus).toBe(400);
  });

  describe('guest endpoints', () => {
    const SESSIONS_URL = 'https://test.api/guest/groups/group-1/sessions';

    beforeEach(() => {
      authService.getTokens.and.returnValue({ idToken: 'user-id-token', accessToken: 'access' });
    });

    it('attaches the signed-in user token to the owner list-sessions endpoint', () => {
      http.get(SESSIONS_URL).subscribe();

      const req = httpMock.expectOne(SESSIONS_URL);
      expect(req.request.headers.get('Authorization')).toBe('Bearer user-id-token');
      req.flush({});
    });

    it('attaches the signed-in user token to the owner delete-session endpoint', () => {
      const url = `${SESSIONS_URL}/session-1`;

      http.delete(url).subscribe();

      const req = httpMock.expectOne(url);
      expect(req.request.headers.get('Authorization')).toBe('Bearer user-id-token');
      req.flush({});
    });

    it('does not replace the guest session token on guest-token endpoints', () => {
      const url = 'https://test.api/guest/sessions/session-1/menu';

      http.get(url, { headers: { Authorization: 'Bearer guest-token' } }).subscribe();

      const req = httpMock.expectOne(url);
      expect(req.request.headers.get('Authorization')).toBe('Bearer guest-token');
      req.flush({});
    });

    it('does not attach the user token to the public group status endpoint', () => {
      const url = 'https://test.api/guest/groups/group-1/status';

      http.get(url).subscribe();

      const req = httpMock.expectOne(url);
      expect(req.request.headers.has('Authorization')).toBeFalse();
      req.flush({});
    });

    it('asks the owner to log in on 401 from the owner sessions endpoint', () => {
      http.get(SESSIONS_URL).subscribe({ error: () => undefined });

      httpMock.expectOne(SESSIONS_URL).flush({}, { status: 401, statusText: 'Unauthorized' });

      expect(toastService.error).toHaveBeenCalledWith(
        'Please log in to perform the action!',
        'Unauthorized'
      );
    });

    it('keeps the guest session-expired message on 401 from a guest-token endpoint', () => {
      const url = 'https://test.api/guest/sessions/session-1/menu';

      http.get(url).subscribe({ error: () => undefined });

      httpMock.expectOne(url).flush({}, { status: 401, statusText: 'Unauthorized' });

      expect(toastService.error).toHaveBeenCalledWith(
        'Session expired. Please re-open the invite link.',
        'Unauthorized'
      );
    });
  });
});
