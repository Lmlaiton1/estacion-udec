import { TestBed } from '@angular/core/testing';

import { MeasurementType } from './measurement-type';

describe('MeasurementType', () => {
  let service: MeasurementType;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MeasurementType);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
