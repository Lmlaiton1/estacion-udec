import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ActuatorForm } from './actuator-form';

describe('ActuatorForm', () => {
  let component: ActuatorForm;
  let fixture: ComponentFixture<ActuatorForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ActuatorForm]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ActuatorForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
