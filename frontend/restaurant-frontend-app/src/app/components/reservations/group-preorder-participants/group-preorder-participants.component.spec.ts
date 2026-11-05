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
      'getOwnerSessionId',
      'deleteSession',
      'submitGroup',
    ]);
    service.getSessions.and.returnValue(of(snapshot));
    service.getOwnerSessionId.and.returnValue(of({ guestSessionId: 'owner-session' }));
    service.deleteSession.and.returnValue(of({ participants: [], total: 0 }));
    service.submitGroup.and.returnValue(of([{ dishId: 'dish-1', quantity: 2 }]));

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
    expect(service.getOwnerSessionId).toHaveBeenCalledWith('reservation-1');
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

  it('submits after confirmation and displays consolidated items', async () => {
    await fixture.whenStable();
    fixture.nativeElement.querySelector('.button--primary').click();
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('.confirmation button')) as HTMLButtonElement[];
    buttons.find((button) => button.textContent?.trim() === 'Підтвердити')?.click();
    fixture.detectChanges();

    expect(service.submitGroup).toHaveBeenCalledWith('group-1', 'reservation-1');
    expect(fixture.nativeElement.textContent).toContain('Груповий pre-order підтверджено');
  });

  it('deletes a guest after confirmation and uses the returned snapshot', async () => {
    await fixture.whenStable();
    fixture.componentRef.setInput('groupReservationId', 'group-1');
    service.getSessions.and.returnValue(of({
      ...snapshot,
      participants: [{ ...snapshot.participants[0], type: 'Guest', id: 'guest-1' }],
      total: 24,
    }));
    fixture.detectChanges();
    fixture.componentInstance.refresh();
    fixture.detectChanges();
    await fixture.whenStable();

    fixture.nativeElement.querySelector('.participant .button--small').click();
    fixture.detectChanges();
    const deleteButton = fixture.nativeElement.querySelector(
      '.confirmation .button--primary'
    ) as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();

    expect(service.deleteSession).toHaveBeenCalledWith('group-1', 'guest-1');
    expect(fixture.componentInstance.snapshot().total).toBe(0);
  });

  it('disables submit when the group is already submitted', () => {
    service.getSessions.and.returnValue(of(snapshot));
    fixture.componentRef.setInput('groupStatus', 'Submitted');
    fixture.componentRef.setInput('groupReservationId', 'submitted-group');
    fixture.detectChanges();

    const submitButton = (Array.from(
      fixture.nativeElement.querySelectorAll('button')
    ) as HTMLButtonElement[])
      .find((button) => button.textContent?.includes('Груповий pre-order')) as HTMLButtonElement;
    expect(submitButton.disabled).toBeTrue();
  });

  it('shows an explicit error when loading fails', () => {
    service.getSessions.and.returnValue(
      throwError(() => new Error('network error'))
    );
    fixture.componentRef.setInput('groupReservationId', 'group-3');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Не вдалося завантажити');
  });

  it('shows a readable submit error for forbidden responses', () => {
    service.submitGroup.and.returnValue(
      throwError(() => ({ status: 403 }))
    );
    fixture.componentInstance.openSubmitConfirmation();
    fixture.componentInstance.submitConfirmed();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Лише Owner');
  });
});
