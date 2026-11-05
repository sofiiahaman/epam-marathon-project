import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CardReservationComponent } from './card-reservation.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { ReservationStatusCustomer } from '@app/models/reservations/reservation.model';

describe('CardReservationComponent', () => {
  let component: CardReservationComponent;
  let fixture: ComponentFixture<CardReservationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }],
      imports: [CardReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CardReservationComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('r', {
      id: 'reservation-1',
      tableId: 'table-1',
      startTime: new Date().toISOString(),
      endTime: new Date().toISOString(),
      guestCount: 2,
      status: ReservationStatusCustomer.Reserved,
      customerId: 'customer-1',
      waiterId: 'waiter-1',
      locationAddress: 'Test location',
      restaurantName: 'Test restaurant',
    });
    fixture.componentRef.setInput('role', 'Customer');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
