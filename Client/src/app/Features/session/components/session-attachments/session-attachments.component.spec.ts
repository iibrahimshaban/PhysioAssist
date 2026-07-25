import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SessionAttachmentsComponent } from './session-attachments.component';

describe('SessionAttachmentsComponent', () => {
  let component: SessionAttachmentsComponent;
  let fixture: ComponentFixture<SessionAttachmentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SessionAttachmentsComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SessionAttachmentsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
