import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SensorForm } from './sensor-form';

describe('SensorForm', () => {
  let component: SensorForm;
  let fixture: ComponentFixture<SensorForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SensorForm]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SensorForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
