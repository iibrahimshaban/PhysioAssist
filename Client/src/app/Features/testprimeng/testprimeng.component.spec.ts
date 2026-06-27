import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TestprimengComponent } from './testprimeng.component';

describe('TestprimengComponent', () => {
  let component: TestprimengComponent;
  let fixture: ComponentFixture<TestprimengComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestprimengComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TestprimengComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
