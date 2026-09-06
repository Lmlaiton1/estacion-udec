import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SensorChartDialog } from './sensor-chart-dialog';

describe('SensorChartDialog', () => {
  let component: SensorChartDialog;
  let fixture: ComponentFixture<SensorChartDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SensorChartDialog]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SensorChartDialog);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
