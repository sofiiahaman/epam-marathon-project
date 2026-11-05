import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, defer, EMPTY, switchMap, tap } from 'rxjs';

import {
  Participant,
  ParticipantsSnapshot,
} from '@app/models/group-preorder/group-preorder.model';
import { GroupPreorderService } from '@app/services/group-preorder/group-preorder.service';
import { getServerErrorMessage } from '@app/shared/helpers/http-error-helpers';

@Component({
  selector: 'app-group-preorder-participants',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './group-preorder-participants.component.html',
  styleUrl: './group-preorder-participants.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupPreorderParticipantsComponent {
  private readonly groupPreorderService = inject(GroupPreorderService);
  private readonly destroyRef = inject(DestroyRef);

  readonly groupReservationId = input.required<string>();
  readonly reservationId = input.required<string>();
  readonly groupStatus = input('Open');
  readonly pollingIntervalMs = input(0);
  readonly refreshCompleted = output<ParticipantsSnapshot>();
  readonly addOwnerDishes = output<string>();

  private readonly refreshVersion = signal(0);
  private readonly loading = signal(true);
  private readonly errorMessage = signal<string | null>(null);
  private readonly ownerSessionId = signal<string | null>(null);
  private readonly ownerSessionError = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly submittedItems = signal<{ dishId: string; quantity: number }[] | null>(null);
  readonly confirmDeleteId = signal<string | null>(null);
  readonly confirmSubmit = signal(false);

  private readonly requestKey = computed(
    () => `${this.groupReservationId()}:${this.refreshVersion()}`
  );

  private readonly snapshotSignal = toSignal(
    toObservable(this.requestKey).pipe(
      switchMap(() =>
        defer(() => {
          this.loading.set(true);
          this.errorMessage.set(null);
          return this.groupPreorderService.getSessions(this.groupReservationId());
        }).pipe(
          tap((snapshot) => {
            this.loading.set(false);
            this.refreshCompleted.emit(snapshot);
          }),
          catchError(() => {
            this.loading.set(false);
            this.errorMessage.set('Не вдалося завантажити учасників. Спробуйте ще раз.');
            return EMPTY;
          })
        )
      )
    ),
    { initialValue: null }
  );

  readonly snapshot = computed<ParticipantsSnapshot>(() =>
    this.snapshotSignalValue() ?? this.snapshotSignal() ?? { participants: [], total: 0 }
  );
  readonly participants = computed(() => this.snapshot().participants);
  readonly isLoading = computed(() => this.loading());
  readonly error = computed(() => this.errorMessage());
  readonly isEmpty = computed(
    () => !this.isLoading() && !this.error() && this.participants().length === 0
  );
  readonly canSubmit = computed(
    () =>
      !this.submitting() &&
      this.groupStatus() !== 'Submitted' &&
      this.snapshot().status !== 'Submitted'
  );
  readonly ownerSessionLoading = computed(
    () => !this.ownerSessionId() && !this.ownerSessionError()
  );
  readonly ownerSessionAvailable = computed(() => this.ownerSessionId() !== null);

  constructor() {
    effect(() => {
      const reservationId = this.reservationId();
      this.ownerSessionId.set(null);
      this.ownerSessionError.set(null);
      this.groupPreorderService
        .getOwnerSessionId(reservationId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: ({ guestSessionId }) => this.ownerSessionId.set(guestSessionId),
          error: (error: HttpErrorResponse) =>
            this.ownerSessionError.set(this.describeError(error, 'отримати owner session')),
        });
    });

    effect((onCleanup) => {
      const intervalMs = this.pollingIntervalMs();
      if (intervalMs <= 0) {
        return;
      }

      const intervalId = window.setInterval(() => this.refresh(), intervalMs);
      onCleanup(() => window.clearInterval(intervalId));
    });
  }

  refresh(): void {
    this.refreshVersion.update((version) => version + 1);
  }

  confirmDelete(sessionId: string): void {
    this.confirmDeleteId.set(sessionId);
  }

  cancelDelete(): void {
    this.confirmDeleteId.set(null);
  }

  deleteConfirmed(): void {
    const sessionId = this.confirmDeleteId();
    if (!sessionId) return;

    this.groupPreorderService
      .deleteSession(this.groupReservationId(), sessionId)
      .subscribe({
        next: (snapshot) => {
          this.confirmDeleteId.set(null);
          this.refreshCompleted.emit(snapshot);
          this.snapshotSignalValue.set(snapshot);
        },
        error: (error: HttpErrorResponse) => {
          this.confirmDeleteId.set(null);
          this.errorMessage.set(this.describeError(error, 'видалити учасника'));
        },
      });
  }

  openSubmitConfirmation(): void {
    if (this.canSubmit()) this.confirmSubmit.set(true);
  }

  cancelSubmit(): void {
    this.confirmSubmit.set(false);
  }

  submitConfirmed(): void {
    const ownerSessionId = this.ownerSessionId();
    if (!ownerSessionId || !this.canSubmit()) return;

    this.submitting.set(true);
    this.submitError.set(null);
    this.groupPreorderService
      .submitGroup(this.groupReservationId(), this.reservationId())
      .subscribe({
        next: (items) => {
          this.submitting.set(false);
          this.confirmSubmit.set(false);
          this.submittedItems.set(items);
          this.snapshotSignalValue.set({
            ...this.snapshot(),
            status: 'Submitted',
          });
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          this.submitError.set(this.describeError(error, 'підтвердити груповий pre-order'));
        },
      });
  }

  openOwnerMenu(): void {
    const sessionId = this.ownerSessionId();
    if (sessionId) this.addOwnerDishes.emit(sessionId);
  }

  private readonly snapshotSignalValue = signal<ParticipantsSnapshot | null>(null);

  private describeError(error: HttpErrorResponse, action: string): string {
    const serverMessage = getServerErrorMessage(error);
    switch (error.status) {
      case 400:
        return serverMessage || `Не вдалося ${action}: перевірте дані групового pre-order.`;
      case 403:
        return serverMessage || `Лише Owner може ${action}.`;
      case 404:
        return serverMessage || 'Групу, бронювання або сесію не знайдено.';
      default:
        return serverMessage || `Не вдалося ${action}. Спробуйте ще раз.`;
    }
  }

  participantTrack(_: number, participant: Participant): string | null {
    return participant.id;
  }
}
