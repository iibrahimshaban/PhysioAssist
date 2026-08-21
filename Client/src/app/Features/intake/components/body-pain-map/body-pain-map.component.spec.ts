import '@angular/compiler';
import { describe, expect, it } from 'vitest';
import { BodyPainMapComponent } from './body-pain-map.component';

describe('BodyPainMapComponent', () => {
  it('pre-fills selections when initialValue is provided', () => {
    const component = new BodyPainMapComponent();

    component.initialValue = {
      regions: [{ id: 'f-head', labelEn: 'Head', labelAr: 'الرأس', severity: 6 }],
    };

    expect(component.payload()).toEqual({
      regions: [{ id: 'f-head', labelEn: 'Head', labelAr: 'الرأس', severity: 6 }],
    });
    expect(component.isSelected('f-head')).toBe(true);
  });
});
