import { TestBed } from '@angular/core/testing';

import { SdTransfer } from '../sd/sd-transfer';

describe('SdTransfer', () => {
  let service: SdTransfer;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SdTransfer);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
