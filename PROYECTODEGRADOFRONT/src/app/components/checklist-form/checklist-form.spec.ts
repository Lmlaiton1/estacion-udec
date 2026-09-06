import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChecklistFormComponent } from './checklist-form';

describe('ChecklistForm', () => {
  let component: ChecklistFormComponent;
  let fixture: ComponentFixture<ChecklistFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChecklistFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ChecklistFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
