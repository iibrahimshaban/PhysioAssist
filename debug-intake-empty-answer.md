# Debug Session: intake-empty-answer

Status: CLOSED (FIXED)
Date: 2026-07-21

Symptoms:
- Intake submission detail showed answer as empty (`—`).
- Stored `formSubmissionData` contained `"value": null`.
- Intake list/detail showed "Unnamed patient".
- Convert to patient returned HTTP 400.

Root Cause:
In `dynamic-form-renderer.component.ts`, an `effect` that reacted to `initialAnswers` was resetting `answers` to `{}` **every single change-detection cycle** (whenever anything in the UI updated), even when `initialAnswers` hadn't changed at all! This erased user input before submission!

Fix Applied:
Modified the effect to only reset `answers` when `initialAnswers` actually changed (using a `prevInitialAnswers` signal to track changes).

Files Modified:
- `Client/src/app/Features/intake/components/dynamic-form-renderer/dynamic-form-renderer.component.ts`: Modified constructor effect to track previous initialAnswers.

Other fixes made earlier:
- Improved backend `ExtractPatientNameSafe` to fall back to first text answer if no default name field exists.
- Modified `ConvertToPatientAsync` to use a placeholder name if no name is found, avoiding 400 errors.
- Improved `SubmittedAnswersViewer`'s `formatAnswerValue` to handle more object formats.
- Fixed renderer text input bindings to update on `input` instead of waiting for `ngModelChange`.
- Added `MapToPreVisitIntakeResponse` in backend to ensure consistent fields (PatientName, PainRegionCount) are returned on all endpoints.
