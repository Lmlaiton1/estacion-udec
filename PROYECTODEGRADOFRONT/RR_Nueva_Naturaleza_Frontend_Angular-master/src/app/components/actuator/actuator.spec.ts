import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Actuator } from './actuator';

describe('Actuator', () => {
  let component: Actuator;
  let fixture: ComponentFixture<Actuator>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Actuator]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Actuator);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
