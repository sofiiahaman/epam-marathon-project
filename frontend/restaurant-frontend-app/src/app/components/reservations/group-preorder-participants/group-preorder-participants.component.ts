import { CurrencyPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, defer, EMPTY, switchMap, tap } from 'rxjs';

import {
  Participant,
  ParticipantsSnapshot,
} from '@app/models/group-preorder/group-preorder.model';
import { GroupPreorderService } from '@app/services/group-preorder/group-preorder.service';

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

  readonly groupReservationId = input.required<string>();
  readonly reservationId = input.required<string>();
  readonly pollingIntervalMs = input(0);
  readonly refreshCompleted = output<ParticipantsSnapshot>();

  private readonly refreshVersion = signal(0);
  private readonly loading = signal(true);
  private readonly errorMessage = signal<string | null>(null);

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
    this.snapshotSignal() ?? { participants: [], total: 0 }
  );
  readonly participants = computed(() => this.snapshot().participants);
  readonly isLoading = computed(() => this.loading());
  readonly error = computed(() => this.errorMessage());
  readonly isEmpty = computed(
    () => !this.isLoading() && !this.error() && this.participants().length === 0
  );

  constructor() {
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

  participantTrack(_: number, participant: Participant): string | null {
    return participant.id;
  }
}
