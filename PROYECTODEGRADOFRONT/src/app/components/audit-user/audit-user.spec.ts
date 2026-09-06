import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuditUser } from './audit-user';

describe('AuditUser', () => {
  let component: AuditUser;
  let fixture: ComponentFixture<AuditUser>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuditUser]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AuditUser);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
