import { TestBed } from '@angular/core/testing';

import { Rolservice } from './rolservice';

describe('Rolservice', () => {
  let service: Rolservice;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Rolservice);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
