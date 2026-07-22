import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PatientOverviewComponent } from './patient-overview.component';

describe('PatientOverviewComponent', () => {
  let component: PatientOverviewComponent;
  let fixture: ComponentFixture<PatientOverviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PatientOverviewComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PatientOverviewComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
