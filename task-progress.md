# Bug Fix Summary

## Bug 1 — Short codes not displaying for Submission ID / Receipt ID
**Root cause:** `PublicIntakeSubmissionResponse` DTO had no `ShortCode` field — only mapped `src.Id` to `dest.SubmissionId`. The confirmation screen bound to `submissionId` (raw GUID).

**Files changed:**
- `PhysioAssist.Api/Modules/Intake/DTOs/PublicAccess/PublicIntakeSubmissionResponse.cs` — Added `ShortCode` property
- `PhysioAssist.Api/Modules/Intake/Mapping/IntakeMappingConfig.cs` — Added `.Map(dest => dest.ShortCode, src => src.ShortCode)` for the public response mapping
- `Client/src/app/Features/intake/models/public-intake.model.ts` — Added `shortCode: string` to frontend model
- `Client/src/app/Features/intake/pages/public-intake/public-intake.component.html` — Changed confirmation screen to show `shortCode` instead of `submissionId`
- `Client/src/app/Features/intake/pages/submission-detail/submission-summary-card/submission-summary-card.component.html` — Changed "Submission ID" display from `details.id` to `details.shortCode`

## Bug 2 — Patient name shows "Unnamed patient" instead of what was typed
**Root cause:** `ExtractPatientNameSafe` hardcoded `"question_default_full_name"` as the question ID. When a doctor customizes the form, question IDs change (they get regenerated with timestamps), so the name lookup returned null. The frontend `submission-detail.component.ts` had the same problem.

**Files changed:**
- `PhysioAssist.Api/Modules/Intake/Helpers/ExtractInputValuesHelper.cs` — Made `ExtractPatientNameSafe` accept an optional schema; dynamically looks up the question ID by text ("Full Name" / "Name") when schema is available, falls back to default ID
- `PhysioAssist.Api/Modules/Intake/Services/IntakeService.cs` — `GetSubmissionsAsync` now pre-loads schemas and passes them to `MapToPreVisitIntakeResponse`; `MapToPreVisitIntakeResponse` accepts an optional schema parameter
- `Client/src/app/Features/intake/pages/submission-detail/submission-detail.component.ts` — `patientNameDisplay` / `patientEmailDisplay` / `patientPhoneDisplay` now use schema-aware question ID lookup

## DB Migration
**No DB migration needed** — the `ShortCode` column already exists in the `PreVisitIntake` table (defined in entity configuration with `HasMaxLength(8)`, indexed, unique). The short code is already being generated and stored on submission creation; it just wasn't being exposed in the response DTO.