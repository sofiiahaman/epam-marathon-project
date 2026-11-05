import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ParticipantsSnapshot } from '@app/models/group-preorder/group-preorder.model';
import { GroupPreorderService } from '@app/services/group-preorder/group-preorder.service';
import { GroupPreorderParticipantsComponent } from './group-preorder-participants.component';

describe('GroupPreorderParticipantsComponent', () => {
  let fixture: ComponentFixture<GroupPreorderParticipantsComponent>;
  let service: jasmine.SpyObj<GroupPreorderService>;

  const snapshot: ParticipantsSnapshot = {
    participants: [
      {
        id: 'owner-session',
        type: 'Owner',
        name: 'Sofiia',
        items: [
          {
            dish: { id: 'dish-1', name: 'Pasta', price: 12 },
            quantity: 2,
          },
        ],
        total: 24,
        isSubmitted: true,
      },
    ],
    total: 24,
  };

  beforeEach(() => {
    service = jasmine.createSpyObj<GroupPreorderService>('GroupPreorderService', [
      'getSessions',
    ]);
    service.getSessions.and.returnValue(of(snapshot));

    TestBed.configureTestingModule({
      imports: [GroupPreorderParticipantsComponent],
      providers: [{ provide: GroupPreorderService, useValue: service }],
    });

    fixture = TestBed.createComponent(GroupPreorderParticipantsComponent);
    fixture.componentRef.setInput('groupReservationId', 'group-1');
    fixture.componentRef.setInput('reservationId', 'reservation-1');
    fixture.detectChanges();
  });

  it('loads and displays participants, dishes, statuses, and totals', () => {
    const text = fixture.nativeElement.textContent;

    expect(service.getSessions).toHaveBeenCalledWith('group-1');
    expect(text).toContain('Sofiia');
    expect(text).toContain('Pasta × 2');
    expect(text).toContain('Підтверджено');
    expect(text).toContain('Group pre-order');
  });

  it('shows the empty state when no participant joined', () => {
    service.getSessions.and.returnValue(of({ participants: [], total: 0 }));
    fixture.componentRef.setInput('groupReservationId', 'group-2');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Поки що ніхто не приєднався');
  });

  it('reloads the snapshot when refresh is clicked', () => {
    const button = fixture.nativeElement.querySelector('button');

    button.click();
    fixture.detectChanges();

    expect(service.getSessions).toHaveBeenCalledTimes(2);
  });

  it('shows an explicit error when loading fails', () => {
    service.getSessions.and.returnValue(
      throwError(() => new Error('network error'))
    );
    fixture.componentRef.setInput('groupReservationId', 'group-3');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Не вдалося завантажити');
  });
});
