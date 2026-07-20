import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SessionActionsComponent } from './session-actions.component';

describe('SessionActionsComponent', () => {
  let component: SessionActionsComponent;
  let fixture: ComponentFixture<SessionActionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SessionActionsComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SessionActionsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
