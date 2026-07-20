import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DictationGuideComponent } from './dictation-guide.component';

describe('DictationGuideComponent', () => {
  let component: DictationGuideComponent;
  let fixture: ComponentFixture<DictationGuideComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DictationGuideComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DictationGuideComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
