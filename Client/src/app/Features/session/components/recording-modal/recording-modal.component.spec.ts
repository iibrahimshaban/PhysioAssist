import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecordingModalComponent } from './recording-modal.component';

describe('RecordingModalComponent', () => {
  let component: RecordingModalComponent;
  let fixture: ComponentFixture<RecordingModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecordingModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(RecordingModalComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
