import '@angular/compiler';
import { describe, expect, it } from 'vitest';
import { BodyPainMapComponent } from './body-pain-map.component';

describe('BodyPainMapComponent', () => {
  it('emits an initial payload when initialValue is provided', () => {
    const component = new BodyPainMapComponent();
    const payloads: unknown[] = [];

    component.mapChange.subscribe((payload) => payloads.push(payload));

    component.initialValue = {
      regions: [{ id: 'f-head', labelEn: 'Head', labelAr: 'الرأس', severity: 6 }],
      chiefComplaint: 'Neck pain',
      patientCategory: 'Orthopedic',
    };

    expect(payloads).toHaveLength(1);
    expect(payloads[0]).toEqual({
      regions: [{ id: 'f-head', labelEn: 'Head', labelAr: 'الرأس', severity: 6 }],
      chiefComplaint: 'Neck pain',
      patientCategory: 'Orthopedic',
    });
  });
});
