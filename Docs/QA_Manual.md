# PhysioAssist — Complete Manual Testing Guide

> **Project:** PhysioAssist  
> **Stack:** .NET 10 Web API + Angular 21 (standalone, zoneless) + SQL Server + PrimeNG 21  
> **Auth:** ASP.NET Core Identity + JWT + Refresh Tokens + Permission-based Authorization  
> **Background Jobs:** Hangfire  
> **AI Services:** Groq (Whisper + Llama), Gemini (transcription)  
> **Media:** Cloudinary (images)  
> **Email:** Ethereal (MailKit via Hangfire)  

---

# Table of Contents

1. [Prerequisites & Setup](#1-prerequisites--setup)
2. [Authentication Module](#2-authentication-module)
3. [Intake Module — Form Schema Management](#3-intake-module--form-schema-management)
4. [Intake Module — QR Generation](#4-intake-module--qr-generation)
5. [Intake Module — Public Intake (Patient-Facing)](#5-intake-module--public-intake-patient-facing)
6. [Intake Module — Submission Review (Doctor-Facing)](#6-intake-module--submission-review-doctor-facing)
7. [Complete End-to-End Workflows](#7-complete-end-to-end-workflows)
8. [Negative Test Cases](#8-negative-test-cases)
9. [Edge Cases](#9-edge-cases)
10. [Database Verification](#10-database-verification)
11. [API Verification](#11-api-verification)
12. [Final Release Checklist](#12-final-release-checklist)

---

# 1. Prerequisites & Setup

## 1.1 Local Environment Requirements

| Dependency | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` |
| Node.js | 20.x+ | `node --version` |
| npm | 11.x+ | `npm --version` |
| SQL Server | 2019+ | LocalDB, Express, or Developer Edition |
| Angular CLI | 21.x | `ng version` |
| Git | Any | `git --version` |

## 1.2 Configuration

### Backend (`PhysioAssist.Api/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PhysioAssist;Trusted_Connection=True;TrustServerCertificate=True;",
    "HangfireConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PhysioAssist;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "PhysioAssist",
    "Audience": "PhysioAssist",
    "ExpiryMinutes": 30
  },
  "AllowedOrigins": ["https://localhost:4200"],
  "QR": {
    "SigningKey": "YourQRSecretKeyAtLeast32CharactersLong!!"
  }
}
```

### Frontend (`Client/src/environments/environment.ts`)

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://localhost:7097/api/'
};
```

## 1.3 Startup Sequence

```bash
# 1. Start backend (from PhysioAssist.Api/)
dotnet run

# 2. Start frontend (from Client/)
npm start
```

### URLs

| Service | URL |
|---|---|
| Backend API | `https://localhost:7097/api/` |
| Swagger UI | `https://localhost:7097/swagger` |
| Hangfire Dashboard | `https://localhost:7097/jobs` (Admin/iti) |
| Frontend App | `https://localhost:4200` |

### Seed Data

On first startup, the backend seeds:
- **Roles:** `Admin`, `SoloDoctor`
- **Admin User:** `Admin@PhysioAssist.com` / `P@ssord123` (with ALL permissions)

---

# 2. Authentication Module

## 2.1 Registration

### Purpose
Allow a new doctor to create an account.

### Prerequisites
- Backend running
- No existing user with the same email

### Step-by-Step

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Send `POST /api/Auth/registration` with valid data | `201 Created` |
| 2 | Check email inbox for confirmation OTP | Email sent via Ethereal |
| 3 | Send `POST /api/Auth/confirm-email` with OTP | `200 OK` — email confirmed |
| 4 | Send `POST /api/Auth/login` with credentials | `200 OK` — JWT + refresh token returned |

### Registration Request

```
POST https://localhost:7097/api/Auth/registration
Content-Type: multipart/form-data

{
  "Email": "doctor@example.com",
  "Password": "P@ssword123",
  "FirstName": "John",
  "LastName": "Smith",
  "UserName": "drjohn",
  "ClinicName": "Smith Clinic",
  "ProfilePhoto": [optional image file]
}
```

### Expected Backend Behavior
1. Creates `ApplicationUser` in `auth.AspNetUsers` table
2. Uploads profile photo to Cloudinary (if provided)
3. Creates `Doctor` record in `auth.Doctors` table
4. Creates OTP entry in `auth.OtpEntries` table
5. Enqueues email via Hangfire
6. Email sent via Ethereal SMTP

### Expected Response (`AuthResponse`)

```json
{
  "id": "guid",
  "firstName": "John",
  "lastName": "Smith",
  "email": "doctor@example.com",
  "userName": "drjohn",
  "token": "jwt-string",
  "expiresIn": 1800,
  "refreshToken": "refresh-token-string",
  "refreshTokenExpiryDate": "2026-10-02T...",
  "profilePictureUrl": "https://res.cloudinary.com/..."
}
```

### Database Changes
| Table | Change |
|---|---|
| `auth.AspNetUsers` | INSERT new user (EmailConfirmed=false) |
| `auth.Doctors` | INSERT new doctor linked to user |
| `auth.OtpEntries` | INSERT OTP with Purpose=EmailVerification |
| `auth.AspNetUserRoles` | INSERT (user added to SoloDoctor role) |
| `auth.AspNetRoleClaims` | No change (role claims already seeded) |
| Hangfire tables | INSERT background job for email |

### Possible Failure Scenarios
| Scenario | Expected Error |
|---|---|
| Duplicate email | `409 Conflict` — DuplicatedEmail |
| Weak password | `400 Bad Request` — validation errors |
| Missing required fields | `400 Bad Request` — FluentValidation |
| Invalid file type | `400 Bad Request` — invalid image |

### Negative Test
- Register with `Password: "123"` → `400` — password too short, missing uppercase/digit
- Register with existing email → `409` — DuplicatedEmail
- Register without ClinicName → `400` — validation
- Register with invalid email → `400` — validation

---

## 2.2 Email Confirmation

### Purpose
Verify the user's email address using a 6-digit OTP.

### Prerequisites
- Registration completed
- OTP sent to email

### Step-by-Step

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Check Hangfire dashboard (`/jobs`) for email job | Job completed |
| 2 | Check Ethereal email inbox for OTP | Email received with 6-digit code |
| 3 | Send `POST /api/Auth/confirm-email` with email + OTP | `200 OK` |
| 4 | Login without confirming email first | `401 Unauthorized` — EmailNotConfirmed |

### Request

```json
POST https://localhost:7097/api/Auth/confirm-email
{
  "email": "doctor@example.com",
  "code": "123456"
}
```

### Database Changes
| Table | Change |
|---|---|
| `auth.AspNetUsers` | UPDATE `EmailConfirmed=true` |
| `auth.OtpEntries` | UPDATE `IsUsed=true` |

### Negative Tests
- Wrong OTP code → `400 Bad Request` — InvalidCode
- Expired OTP (older than OTP expiry) → `400` — InvalidCode
- Confirm email for non-existent user → `404` — UserNotFound
- Confirm email that's already confirmed → `400` — DuplicatedConfirmation
- Resend confirmation `POST /api/Auth/resend-confirmation-email` → new OTP sent, old one invalidated

---

## 2.3 Login

### Purpose
Authenticate and receive JWT + refresh token.

### Prerequisites
- User registered and email confirmed

### Request

```json
POST https://localhost:7097/api/Auth/login
{
  "email": "Admin@PhysioAssist.com",
  "password": "P@ssord123"
}
```

### Expected Response (`AuthResponse`)
- `200 OK`
- JWT token (expires in 30 minutes by default)
- Refresh token (expires in 90 days)

### Expected Backend Behavior
1. Validates credentials via `SignInManager.PasswordSignInAsync`
2. Checks `IsDisabled=false`, `EmailConfirmed=true`, not locked out
3. Generates JWT with claims: `sub`, `email`, `given_name`, `family_name`, `jti`, `Roles`, `Permissions`
4. Generates refresh token (90-day expiry)
5. Returns AuthResponse

### Database Changes
| Table | Change |
|---|---|
| `auth.RefreshTokens` | INSERT new refresh token (UserId, Token, ExpiresOn) |

### Negative Tests
| Scenario | Expected |
|---|---|
| Wrong password | `401 Unauthorized` — InvalidCredentials |
| Disabled user | `401 Unauthorized` — DisabledUser |
| Unconfirmed email | `401 Unauthorized` — EmailNotConfirmed |
| Locked out user | `401 Unauthorized` — LockedUser |

---

## 2.4 Token Refresh

### Purpose
Obtain a new JWT without re-authentication.

### Request

```json
POST https://localhost:7097/api/Auth/new-refresh
{
  "token": "expired-or-valid-jwt",
  "refreshToken": "current-refresh-token"
}
```

### Expected Behavior
- Validates the old JWT (ignoring expiry, checking signature + claims)
- Finds the active refresh token in DB
- Revokes the old refresh token
- Creates a new refresh token
- Returns new AuthResponse

### Database Changes
| Table | Change |
|---|---|
| `auth.RefreshTokens` | UPDATE old token `RevokedOn=now` |
| `auth.RefreshTokens` | INSERT new token |

### Negative Tests
- Reuse a revoked refresh token → `400` — InvalidRefresh
- Send invalid JWT → `400` — InvalidJwtToken

---

## 2.5 Password Reset

### Purpose
Reset password via email OTP.

### Steps
1. `POST /api/Auth/forget-password` with email → OTP sent
2. `POST /api/Auth/reset-password` with email + OTP + new password → success

### Negative Tests
- Wrong OTP → `400` — InvalidCode
- Weak new password → `400` — validation errors

---

## 2.6 Token Validation

JWT tokens include these claims:

| Claim | Value |
|---|---|
| `sub` | User GUID |
| `email` | User email |
| `given_name` | First name |
| `family_name` | Last name |
| `jti` | Unique token ID |
| `Roles` | JSON array of role names |
| `Permissions` | JSON array of permission strings |

All API endpoints (except Auth and Public intake) require a valid JWT via the `Authorization: Bearer <token>` header.

---

# 3. Intake Module — Form Schema Management

## 3.1 Create Form Schema

### Page/URL
- **URL:** `https://localhost:4200/intake/schemas/new`
- **Navigation:** Login → Click "Schemas" in header → Click "Create New Schema"
- **Required Role:** Any authenticated user with `IntakeManageForms` permission

### Page Elements
| Element | Description |
|---|---|
| Schema name input | Text field (required, max 150 chars) |
| Description textarea | Optional (max 500 chars) |
| Set as default toggle | Checkbox |
| Sections tree (left panel) | Shows sections and groups |
| Form preview (center) | Live preview of the form |
| Property panel (right) | Edit selected item properties |
| Save button | Saves as Draft |
| Add Section button | Adds a new section |
| Add Group button | Adds a group to selected section |
| Add Question button | Adds a question to selected group |

### Step-by-Step — Create a Schema

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to `/intake/schemas/new` | Empty builder shown |
| 2 | Enter "Patient Intake Form" as name | Name field populated |
| 3 | Add a section "Medical History" | Section appears in tree and preview |
| 4 | Add a group "General" under section | Group appears nested |
| 5 | Add a question "Full Name" (type: text, required) | Question appears in group |
| 6 | Add a question "Age" (type: number, min: 0, max: 150) | Number input with validation |
| 7 | Add a question "Has allergies?" (type: radio, options: Yes,No) | Radio buttons shown |
| 8 | Add a question "Describe pain" with condition: show if "Has allergies"="Yes" | Condition arrow in preview |
| 9 | Click "Save" | Schema saved as Draft |
| 10 | Navigate to `/intake/schemas` | New schema listed with "Draft" tag |

### API Call

```
POST https://localhost:7097/api/intake/form-schemas
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "name": "Patient Intake Form",
  "description": "Standard intake questionnaire",
  "schemaJson": "{...DynamicFormSchemaDto...}",
  "isDefault": false
}
```

### Expected Backend Behavior
1. Deserializes schema JSON into `DynamicFormSchemaDto`
2. Validates schema: unique IDs, valid question types, valid validation rules, valid condition operators
3. Checks for duplicate name per doctor
4. If `isDefault=true`, unsets other defaults for this doctor
5. Computes SHA-256 hash of schema JSON
6. Creates `PatientFormSchema` with `Status=Draft`, `Version=1`

### Expected Response (`201 Created`)

```json
{
  "id": "guid",
  "name": "Patient Intake Form",
  "description": "Standard intake questionnaire",
  "status": 0,
  "version": 1,
  "isDefault": false,
  "schemaHash": "base64-hash",
  "createdAt": "2026-07-04T..."
}
```

### Database Changes
| Table | Change |
|---|---|
| `intake.PatientFormSchemas` | INSERT new schema |
| `intake.PatientFormSchemas` | UPDATE `IsDefault=false` for others (if this is new default) |

### Negative Tests
- Empty name → `400` — Name required
- Name > 150 chars → `400` — MaxLength validation
- Duplicate name for same doctor → `409` — DuplicateSchemaName
- Invalid SchemaJson (malformed JSON) → `400` — InvalidSchemaJson
- Missing SchemaJson → `400` — Required
- Question with invalid type → `400` — Unknown QuestionType
- Validation rule with invalid type → `400` — Unknown ValidationRuleType

---

## 3.2 Update Form Schema

### Page/URL
- **URL:** `https://localhost:4200/intake/schemas/edit/:id`
- **Navigation:** Schema List → Click a schema row
- **Required Role:** `IntakeManageForms`

### Step-by-Step

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to schema list | Table shows all schemas |
| 2 | Click a Draft schema | Opens builder with data loaded |
| 3 | Edit name, add questions, modify rules | Changes reflected in preview |
| 4 | Click "Save" | Schema updated, version incremented |

### API Call

```
PUT https://localhost:7097/api/intake/form-schemas/{schemaId}
Authorization: Bearer <jwt>
```

### Database Changes
| Table | Change |
|---|---|
| `intake.PatientFormSchemas` | UPDATE record, version incremented |

### Negative Tests
- Edit a Published schema → `400` — SchemaAlreadyPublished
- Edit non-existent schema → `404` — SchemaNotFound

---

## 3.3 Publish Form Schema

### Purpose
Make a schema available for public intake via QR.

### Step-by-Step

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Navigate to schema list | Schemas displayed |
| 2 | Find a Draft schema | "Draft" tag visible |
| 3 | Click "Publish" button | Confirmation dialog |
| 4 | Confirm publish | Status changes to "Published" tag |

### API Call

```
POST https://localhost:7097/api/intake/form-schemas/{schemaId}/publish
Authorization: Bearer <jwt>
```

### Expected Backend Behavior
1. Sets `Status=Published` (1)
2. Records `PublishedAt=DateTime.UtcNow`
3. Increments version
4. Idempotent: if already published, returns existing without changes

### Database Changes
| Table | Change |
|---|---|
| `intake.PatientFormSchemas` | UPDATE `Status=1`, `PublishedAt=now`, `Version+=1` |

### Negative Tests
- Publish non-existent schema → `404`
- Publish from unauthorized doctor → `404` (doctor-scoped query)

---

## 3.4 View Schema List

### Page/URL
- **URL:** `https://localhost:4200/intake/schemas`
- **Navigation:** Login → "Schemas" in header → `/intake`

### Page Elements
| Element | Description |
|---|---|
| Search input | Client-side filter by name |
| Table | Columns: Name, Description, Status, Version, Default, Created |
| Status tags | Draft (blue), Published (green) |
| Publish button | Per-row action |
| QR button | Per-row action (only for Published) |
| Edit button | Per-row action (row click) |
| Paginator | PrimeNG paginator at bottom |

### Expected UI States
| State | Behavior |
|---|---|
| Loading | Skeleton or spinner |
| Empty | "No schemas yet. Create your first schema." |
| Search with no results | "No schemas match your search" |
| Error | Red card with Retry button |

---

## 3.5 QR Generation

### Page/URL
- **Dialog** on Schema List page (triggered by QR button)
- **Required Role:** `QRGenerate`

### Step-by-Step

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Ensure schema is Published | Status tag shows "Published" |
| 2 | Click QR icon on the schema row | QR dialog opens |
| 3 | Enter expiry hours (default 24) | Input validated 1–8760 |
| 4 | Click "Generate" | Token and URL generated |
| 5 | Click "Copy" | URL copied to clipboard |
| 6 | Click "Open" | New tab opens to public intake URL |

### API Call

```
POST https://localhost:7097/api/intake/form-schemas/{schemaId}/qr-link
Authorization: Bearer <jwt>

{
  "expiryHours": 24
}
```

### Expected Response

```json
{
  "token": "base64url-hmac-token",
  "publicUrl": "https://localhost:4200/public/intake/base64url-hmac-token",
  "expiresAt": "2026-07-05T18:00:00Z"
}
```

### Expected Backend Behavior
1. Validates schema is Published
2. Creates `QRTokenPayload` with `Purpose=Intake`, `TargetId=schemaId`, `Expiry=now+expiryHours`
3. HMAC-SHA256 signs the payload
4. Computes SHA-256 hash of token for DB storage
5. Returns token + URL + expiry

### Database Changes
| Table | Change |
|---|---|
| No direct DB change (token is self-contained) |

### Negative Tests
- Generate QR for Draft schema → `400` — SchemaNotPublished
- Generate QR for non-existent schema → `404`
- ExpiryHours = 0 → `400` — validation error
- ExpiryHours > 8760 → `400` — validation error

---

# 4. Intake Module — Public Intake (Patient-Facing)

## 4.1 Access Public Intake Form

### Page/URL
- **URL:** `https://localhost:4200/public/intake/{token}`
- **Navigation:** Doctor generates QR code → Patient scans QR → Opens this page
- **Required Role:** None (anonymous access)

### Page Elements
| Element | Description |
|---|---|
| Form title | Schema name |
| Form description | Schema description |
| Patient name input | Required |
| Patient email input | Optional |
| Patient phone input | Optional |
| Dynamic form questions | Rendered by DynamicFormRendererComponent |
| Pain point selector | Body diagram (if schema includes painpoint type questions) |
| Submit button | Bottom of form |
| Error state | Invalid/expired token message |
| Loading state | Spinner |

### Step-by-Step

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Open `/public/intake/{valid-token}` | Form loads with schema name |
| 2 | Enter patient name | Field populated |
| 3 | Enter email and phone | Optional fields populated |
| 4 | Answer all required questions | Check marks appear |
| 5 | Click body diagram to add pain points (if applicable) | Dots appear on SVG |
| 6 | Set pain intensity | Color changes (green/yellow/red) |
| 7 | Click "Submit" | Loading spinner → Success confirmation |
| 8 | See submission ID displayed | "Submission received" message |

### Expected UI States
| State | Behavior |
|---|---|
| Loading | CSS spinner with "Loading intake form..." |
| Invalid/expired token | Error message: "This intake link is invalid or has expired." |
| Schema not found | Error message: "The requested form is no longer available." |
| Validation errors | Inline error messages on each field |
| Submit success | Confirmation card with submission ID |

### API Calls

**GET Form:**
```
GET https://localhost:7097/api/public/intake/{token}
```

**Expected Response (`200 OK`):**
```json
{
  "formSchemaId": "guid",
  "formName": "Patient Intake Form",
  "formDescription": "Standard intake questionnaire",
  "schemaJson": "{...DynamicFormSchemaDto...}",
  "version": 1
}
```

**Submit Form:**
```
POST https://localhost:7097/api/public/intake/{token}/submit
{
  "patientName": "Jane Doe",
  "patientEmail": "jane@example.com",
  "patientPhone": "+1234567890",
  "formSubmissionData": "{...DynamicFormSubmissionDto...}",
  "painPointsData": "[{\"bodyPart\":\"front\",\"x\":0.5,\"y\":0.3,\"intensity\":7}]"
}
```

**Expected Response (`201 Created`):**
```json
{
  "submissionId": "guid",
  "message": "Your intake form has been submitted successfully.",
  "submittedAt": "2026-07-04T18:00:00Z"
}
```

### Expected Backend Behavior (Submission)
1. Validates QR token (HMAC + expiry + purpose)
2. Deserializes submission JSON
3. Validates submission against schema (cross-references section/group/question IDs)
4. Validates attachment metadata
5. Creates `PreVisitIntake` record with `Status=Pending`
6. Stores hashed token for audit

### Database Changes
| Table | Change |
|---|---|
| `intake.PreVisitIntakes` | INSERT new submission with `Status=0 (Pending)` |
| `intake.PreVisitIntakes` | SET `AccessTokenHash`, `SubmittedAt`, `ExpiresAt` |

---

# 5. Intake Module — Submission Review (Doctor-Facing)

## 5.1 View Submission List

### Page/URL
- **URL:** `https://localhost:4200/intake/submissions`
- **Navigation:** Login → Click "Submissions" in header
- **Required Role:** `IntakeReview`

### Page Elements
| Element | Description |
|---|---|
| Search input | Client-side filter by patient name or email |
| Status filter dropdown | All, Pending, Submitted, In Review, Approved, Rejected, Converted, Expired |
| Table | Columns: Patient Name, Email, Phone, Status, Submitted |
| Status tags | Color-coded per status (blue=info, orange=warn, green=success, gray=secondary) |
| Paginator | PrimeNG paginator |
| Refresh button | Top-right |
| Row click | Navigate to detail page |

### Expected UI States
| State | Behavior |
|---|---|
| Loading | Table skeleton |
| Empty (no filters) | "No submissions received yet" |
| Empty (with filters) | "No submissions match your filters" |
| Error | Red card with Retry button |
| Row hover | Highlighted row |

### Status Display Reference
| Backend Status | UI Label | Tag Color |
|---|---|---|
| Pending (0) | Pending | Blue (info) |
| Submitted (1) | Submitted | Blue (info) |
| InReview (2) | In Review | Orange (warn) |
| Approved (3) | Approved | Orange (warn) |
| Rejected (4) | Rejected | Gray (secondary) |
| Converted (5) | Converted | Green (success) |
| Expired (6) | Expired | Gray (secondary) |

---

## 5.2 View Submission Detail

### Page/URL
- **URL:** `https://localhost:4200/intake/submissions/:id`
- **Navigation:** Submission List → Click a row
- **Required Role:** `IntakeReview`

### Page Elements
| Element | Description |
|---|---|
| Back button | Top-left, navigates to list |
| Patient info card | Name, email, phone, submitted date, submission ID |
| Status tag | Top-right of patient card |
| Action buttons | Based on current status (see below) |
| Form details card | Form name, version, reviewed date |
| Submitted answers card | Sectioned answer display with question text |
| Pain points card | Body SVG + intensity list (if pain points exist) |
| Convert dialog | Modal with patient info summary |

### Action Buttons by Status
| Current Status | Available Actions |
|---|---|
| Pending (0) | **Approve** (sends Approved=3 via PATCH status) |
| Approved (3) | **Convert to Patient** (opens dialog → calls POST convert-to-patient), **Reject** (sends Rejected=4 via PATCH status) |
| All others | No actions (terminal states) |

### Step-by-Step — Approve a Submission

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Find a submission with "Pending" status | Tag shows "Pending" |
| 2 | Click the row | Detail page loads |
| 3 | Review patient info and answers | All data visible |
| 4 | Click "Approve" button | Confirmation dialog |
| 5 | Click "Yes, Proceed" | Button loading → success toast |
| 6 | Status tag changes to "Approved" | Orange tag appears |
| 7 | New action buttons appear | "Convert to Patient" and "Reject" |

### Step-by-Step — Reject a Submission

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Find Approved submission | Detail page |
| 2 | Click "Reject" | Confirmation dialog |
| 3 | Confirm | Status changes to "Rejected" (gray) |
| 4 | All action buttons disappear | No actions, terminal state |

### Step-by-Step — Convert to Patient

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Find Approved submission | Detail page |
| 2 | Click "Convert to Patient" | Modal dialog opens |
| 3 | Review patient details in dialog | Name, email, phone shown |
| 4 | Click "Confirm Conversion" | Loading → error toast: "Could not convert" |

> **Note:** The `ConvertToPatient` endpoint is **stubbed**. It always returns `503 ConversionDependencyMissing`. This is expected behavior — the Patient module is not yet fully implemented.

### API Calls

**Get Submission Details:**
```
GET https://localhost:7097/api/intake/submissions/{submissionId}
Authorization: Bearer <jwt>
```

**Expected Response:**
```json
{
  "id": "guid",
  "doctorId": "guid",
  "formSchemaId": "guid",
  "formSchemaVersion": 1,
  "patientName": "Jane Doe",
  "patientEmail": "jane@example.com",
  "patientPhone": "+1234567890",
  "formSubmissionData": "{...DynamicFormSubmissionDto...}",
  "painPointsData": "[{\"bodyPart\":\"front\",\"x\":0.5,\"y\":0.3,\"intensity\":7}]",
  "status": 0,
  "submittedAt": "2026-07-04T18:00:00Z",
  "formSchemaName": "Patient Intake Form"
}
```

**Update Status:**
```
PATCH https://localhost:7097/api/intake/submissions/{submissionId}/status
Authorization: Bearer <jwt>

{
  "newStatus": 3
}
```

**Expected Response (`200 OK`):**
```json
{
  "id": "guid",
  "status": 3,
  ...
}
```

**Convert to Patient:**
```
POST https://localhost:7097/api/intake/submissions/{submissionId}/convert-to-patient
Authorization: Bearer <jwt>

{}
```

**Expected Response (`503 Service Unavailable`):**
```json
{
  "status": 503,
  "title": "Service Unavailable",
  "detail": "Conversion dependency is missing. Patient module is not yet integrated."
}
```

### Database Changes (Status Update)
| Table | Change |
|---|---|
| `intake.PreVisitIntakes` | UPDATE `Status`, `ReviewedAt=now`, `ReviewedByDoctorId` |

---

# 6. Complete End-to-End Workflows

## Flow 1: Doctor Registration → Complete Intake Cycle

### Prerequisites
- Clean database (run `dotnet run` fresh)
- Ethereal email accessible for OTP

### Steps

| Step | User | Action | Expected UI/API |
|------|------|--------|-----------------|
| 1.1 | Doctor | Navigate to `https://localhost:4200` | Login page not shown (no auth guard exists yet — header shown) |
| 1.2 | Developer | Send `POST /api/Auth/registration` via Swagger or curl | `201 Created` + AuthResponse |
| 1.3 | Developer | Check Hangfire dashboard `/jobs` | Email job completed |
| 1.4 | Developer | Access Ethereal email → copy OTP | OTP visible in email |
| 1.5 | Developer | Send `POST /api/Auth/confirm-email` with OTP | `200 OK` |
| 1.6 | Doctor | Send `POST /api/Auth/login` with credentials | `200 OK` — JWT + refresh |
| 1.7 | Doctor | Navigate to `/intake/schemas` | Empty schema list |
| 1.8 | Doctor | Click "Create New Schema" → `/intake/schemas/new` | Empty builder |
| 1.9 | Doctor | Enter "General Intake" as name | Field populated |
| 1.10 | Doctor | Add section "Patient Info" | Tree updated |
| 1.11 | Doctor | Add group "Personal Details" | Nested under section |
| 1.12 | Doctor | Add questions: Full Name (text, required), Age (number, min:0, max:150), Gender (select: Male/Female/Other), Has Pain? (radio: Yes/No) | All questions visible in tree + preview |
| 1.13 | Doctor | Add "Pain Location" (painpoint) with condition: show if "Has Pain?=Yes" | Condition arrow shown |
| 1.14 | Doctor | Click "Save" | Toast "Saved successfully" |
| 1.15 | Doctor | Navigate to `/intake/schemas` | Schema listed as "Draft" |
| 1.16 | Doctor | Click "Publish" on the schema | Confirm dialog |
| 1.17 | Doctor | Confirm | Status changes to "Published" |
| 1.18 | Doctor | Click QR icon on published schema | QR dialog |
| 1.19 | Doctor | Set expiry: 48 hours, click "Generate" | Token + URL generated |
| 1.20 | Doctor | Click "Copy" | URL copied to clipboard |
| 1.21 | Doctor | Paste URL in new browser tab (or incognito) | Public intake form loads |
| 1.22 | Patient | Enter name "Jane Doe", email "jane@test.com" | Fields populated |
| 1.23 | Patient | Answer all questions: Full Name=Jane, Age=30, Gender=Female, Has Pain?=Yes | Form validates |
| 1.24 | Patient | Pain location section appears (condition met) | Body diagram visible |
| 1.25 | Patient | Click body front at shoulder area | Red dot appears |
| 1.26 | Patient | Set intensity to 7 | Dot turns red |
| 1.27 | Patient | Click "Submit" | Loading → "Submission received: ID: xxx" |
| 1.28 | Doctor | Navigate to `/intake/submissions` | New submission shows "Pending" |
| 1.29 | Doctor | Click the row | Detail page loads |
| 1.30 | Doctor | Review answers and pain points | All data visible |
| 1.31 | Doctor | Click "Approve" → confirm | Status changes to "Approved" |
| 1.32 | Doctor | Click "Convert to Patient" → review dialog → confirm | Error toast: "Conversion dependency missing" (expected stub) |

---

## Flow 2: Form Builder — Advanced Features

### Steps

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Create schema with multiple sections | All sections render in preview |
| 2 | Add validation rules: regex on phone field, min/max on number field | Validation applied |
| 3 | Add conditions: AND group with 2 conditions, OR group with 2 conditions | Both evaluated |
| 4 | Save, publish, generate QR, open as patient | Conditions evaluated correctly |
| 5 | Submit with invalid data (phone=abc) | Client-side validation errors |
| 6 | Submit with valid data | Success |

---

## Flow 3: Status Transitions

| From | Action | To | API Endpoint |
|---|---|---|---|
| Pending (0) | Approve | Approved (3) | `PATCH /status { newStatus: 3 }` |
| Approved (3) | Reject | Rejected (4) | `PATCH /status { newStatus: 4 }` |
| Approved (3) | Convert | Converted (5) | `POST /convert-to-patient` (stubbed) |

### Verifying State Machine Integrity
1. Create submission via public intake → `Status=0 (Pending)`
2. Try to Reject directly from Pending → Not possible (no button)
3. Approve → `Status=3 (Approved)`
4. Try to Approve again → Not possible (no button)
5. Reject → `Status=4 (Rejected)` — terminal state, no actions
6. Create another submission, Approve → Convert → `ConversionDependencyMissing` (stub)

---

## Flow 4: Token Security

### Steps

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Generate QR with 1-hour expiry | Token valid |
| 2 | Immediately access public intake | Form loads |
| 3 | Wait for token to expire (or set expiry short) | "This intake link is invalid or has expired." |
| 4 | Tamper with token (change last character) | "This intake link is invalid or has expired." |
| 5 | Access with completely fake token | "This intake link is invalid or has expired." |

---

# 7. Dynamic Form Renderer — Question Types

## Supported Types

| Type | UI Control | Validation |
|---|---|---|
| `text` | `<input type="text">` | required, minLength, maxLength, pattern |
| `textarea` | `<textarea>` | required, minLength, maxLength |
| `number` | `<p-inputNumber>` | required, min, max |
| `email` | `<input type="email">` | required, email |
| `phone` | `<input type="tel">` | required, pattern |
| `date` | `<input type="date">` | required |
| `select` | `<p-select>` | required |
| `multiselect` | Rendered as select (backend type, UI uses select) | required |
| `radio` | `<p-selectButton>` | required |
| `checkbox` | `<p-checkbox>` | required (must be checked) |
| `boolean` | Rendered as select (backend type) | required |
| `file` | **Disabled** — shows "File upload is not supported yet" | — |
| `bodyselector` | `<app-body-selector>` | required (at least 1 point) |
| `painpoint` | Rendered as bodyselector | required |

### Test Each Type

| Step | Action | Expected Result |
|------|--------|----------------|
| 1 | Create schema with one question per type | All types added |
| 2 | Publish + QR + open as patient | All controls render |
| 3 | Test `text` — enter text, clear → validation error | Required error shown |
| 4 | Test `number` — enter "abc" → input blocks, try 151 with max=150 → error | Validation works |
| 5 | Test `email` — enter "notanemail" → error | Email validation |
| 6 | Test `select` — choose option → value set | Selection works |
| 7 | Test `radio` — toggle between options | Radio selection |
| 8 | Test `checkbox` — check/uncheck | Boolean toggle |
| 9 | Test `bodyselector` — click body → dot appears, set intensity | Pain point registered |
| 10 | Submit with all valid data | Success |
| 11 | Submit with missing required → validation errors per field | Errors shown, not submitted |

---

# 8. Negative Test Cases

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 1 | **Invalid Login** | POST login with wrong password | `401 InvalidCredentials` |
| 2 | **Expired QR** | Set expiry to 1 hour, wait 1 hour, access link | Error: "This intake link is invalid or has expired." |
| 3 | **Deleted Schema** | Doctor creates schema → deletes via DB (not UI) → generate QR | Schema not found |
| 4 | **Duplicate Submission** | Submit same token twice | First OK, second creates new submission (tokens are single-use by hash, but same schema can be used multiple times) |
| 5 | **Empty Required Fields** | Submit public form with empty required patient name | Validation error |
| 6 | **Unauthorized Access** | Access `/intake/submissions` without JWT | `401 Unauthorized` |
| 7 | **Expired JWT** | Wait 30+ minutes, submit request with expired token | `401 InvalidJwtToken` |
| 8 | **Invalid JWT Signature** | Modify JWT payload, send request | `401 InvalidJwtToken` |
| 9 | **Invalid Status Transition** | PATCH `/status` with `newStatus: 99` | `400` — IsInEnum validation |
| 10 | **Missing Permission** | User without `IntakeReview` accesses submissions | `403 Forbidden` |
| 11 | **Tampered Token** | Modify QR token, try to access form | Invalid token error |
| 12 | **Wrong Schema ID in Submission** | Submit formSubmissionData with non-existent section ID | `400 InvalidSubmissionStructure` |
| 13 | **Double Submit** | Click submit twice rapidly | First succeeds, second creates duplicate submission (no idempotency) |
| 14 | **Blank Schema Name** | Create schema with empty name | `400` validation error |
| 15 | **Oversized Text** | Enter 5000 characters in a 200-char max field | Client-side maxLength validation |

---

# 9. Edge Cases

| # | Edge Case | How to Test | Expected Result |
|---|-----------|-------------|-----------------|
| 1 | **Very long patient name** | Enter 200+ characters | Truncated at 200 or validation error |
| 2 | **Very long question text** | Create a question with 500 chars of text | Truncates or wraps in UI |
| 3 | **Thousands of pain points** | Add 100+ points on body diagram | UI may lag, but all points rendered |
| 4 | **Network interruption** | Disconnect Wi-Fi while submitting public form | Error state with retry |
| 5 | **Page refresh while submitting** | Press F5 after clicking submit | Form resets, no partial submission |
| 6 | **Browser back button** | Navigate to list → detail → press back | Returns to list page |
| 7 | **Browser forward button** | After going back from detail → press forward | Returns to detail page |
| 8 | **Very long form** | Create schema with 50+ sections and 200+ questions | Scrolling works, performance acceptable |
| 9 | **Session expiration during review** | Wait 30 min, then try to approve | JWT expired → `401` redirect to login |
| 10 | **Concurrent schema edit** | Two doctors edit same schema simultaneously | Last save wins (no optimistic concurrency) |
| 11 | **Multiple browsers** | Open same public intake URL in Chrome + Firefox | Both can submit independently |
| 12 | **Zero questions** | Create section with no questions | Section renders empty |
| 13 | **All conditions false** | Set up form where ALL conditions are false | Only unconditional fields shown |
| 14 | **Mobile viewport** | Open public intake on mobile (375px width) | Responsive layout, no horizontal scroll |
| 15 | **Special characters in name** | Name: `<script>alert('xss')</script>` | Escaped, rendered as text |

---

# 10. Database Verification

## Tables by Schema

### Schema: `auth`
| Table | Purpose | Verified By |
|---|---|---|
| `AspNetUsers` | User accounts | Registration, Login |
| `AspNetRoles` | Roles (Admin, SoloDoctor) | Seed data |
| `AspNetUserRoles` | User-role assignment | Registration |
| `AspNetRoleClaims` | Permission claims | Seed data |
| `Doctors` | Doctor profiles | Registration |
| `RefreshTokens` | JWT refresh tokens | Login, Refresh |
| `OtpEntries` | Email verification codes | Registration, Confirm Email |

### Schema: `intake`
| Table | Purpose | Verified By |
|---|---|---|
| `PatientFormSchemas` | Form schema definitions | Create/Update/Publish schema |
| `PreVisitIntakes` | Patient submissions | Submit intake, List/View submissions |

### Schema: `scheduling`
| Table | Purpose | Status |
|---|---|---|
| `ScheduleSlot` | Doctor schedule slots | Entity only — no API |

### Schema: `session`
| Table | Purpose | Status |
|---|---|---|
| `Sessions` | Patient sessions | Entity only — no API |
| `SessionTranscriptions` | Session audio transcripts | Entity only — no API |
| `SessionTranscriptionChunks` | Transcript chunks with vectors | Entity only — no API |

### Schema: `patient`
| Table | Purpose | Status |
|---|---|---|
| `Patients` | Patient records | Entity only — no API |
| `DoctorPatients` | Doctor-patient assignments | Entity only — no API |

### Schema: `initialreport`
| Table | Purpose | Status |
|---|---|---|
| `InitialReports` | Initial patient reports | Entity only — no API |
| `ReportAttachments` | Report file attachments | Entity only — no API |

### Schema: `shared`
| Table | Purpose | Status |
|---|---|---|
| `Notifications` | System notifications | Entity only — no API |

### Schema: `dbo` (default)
| Table | Purpose | Verified By |
|---|---|---|
| `__EFMigrationsHistory` | EF migration tracking | Startup |
| Hangfire tables | Background job storage | Email sending |

## Key SQL Queries for Verification

```sql
-- Verify user registration
SELECT Id, Email, FirstName, LastName, EmailConfirmed, IsDisabled
FROM auth.AspNetUsers
WHERE Email = 'doctor@example.com';

-- Verify OTP
SELECT Code, Purpose, ExpiresAt, IsUsed
FROM auth.OtpEntries
WHERE UserId = '<user-guid>';

-- Verify refresh token
SELECT Token, ExpiresOn, RevokedOn
FROM auth.RefreshTokens
WHERE UserId = '<user-guid>' AND RevokedOn IS NULL;

-- Verify form schema
SELECT Id, Name, Status, Version, IsDefault, PublishedAt
FROM intake.PatientFormSchemas
WHERE DoctorId = '<doctor-guid>';

-- Verify submissions
SELECT Id, PatientName, Status, SubmittedAt, ReviewedAt
FROM intake.PreVisitIntakes
WHERE DoctorId = '<doctor-guid>';

-- Verify submission after status update
SELECT Id, Status, ReviewedAt, ReviewedByDoctorId
FROM intake.PreVisitIntakes
WHERE Id = '<submission-guid>';
```

---

# 11. API Verification

## Authentication Endpoints

### `POST /api/Auth/registration`
| Aspect | Value |
|---|---|
| Request | `multipart/form-data` with RegistrationRequest fields |
| Success | `201 Created` with `AuthResponse` |
| Failure | `400` (validation), `409` (duplicate email) |
| Auth | None |

### `POST /api/Auth/confirm-email`
| Aspect | Value |
|---|---|
| Request | `{ email: string, code: string }` |
| Success | `200 OK` |
| Failure | `400` (invalid code), `404` (user not found) |
| Auth | None |

### `POST /api/Auth/login`
| Aspect | Value |
|---|---|
| Request | `{ email: string, password: string }` |
| Success | `200 OK` with `AuthResponse` |
| Failure | `401` (invalid credentials) |
| Auth | None |

### `POST /api/Auth/new-refresh`
| Aspect | Value |
|---|---|
| Request | `{ token: string, refreshToken: string }` |
| Success | `200 OK` with new `AuthResponse` |
| Failure | `400` (invalid/revoked refresh token) |
| Auth | None (uses refresh token) |

### `POST /api/Auth/revoke-refresh-token`
| Aspect | Value |
|---|---|
| Request | `{ token: string, refreshToken: string }` |
| Success | `200 OK` |
| Failure | `400` |

### `POST /api/Auth/forget-password`
| Aspect | Value |
|---|---|
| Request | `{ email: string }` |
| Success | `200 OK` — OTP sent |

### `POST /api/Auth/reset-password`
| Aspect | Value |
|---|---|
| Request | `{ email: string, newPassword: string, otp: string }` |
| Success | `200 OK` |

## Intake Endpoints

### `POST /api/intake/form-schemas`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeManageForms` |
| Request | `{ name, description?, schemaJson, isDefault? }` |
| Success | `201 Created` with `FormSchemaResponse` |
| Failure | `400` (validation), `409` (duplicate name) |

### `PUT /api/intake/form-schemas/{id}`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeManageForms` |
| Success | `200 OK` |
| Failure | `404` (not found), `400` (already published) |

### `POST /api/intake/form-schemas/{id}/publish`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeManageForms` |
| Success | `200 OK` |
| Failure | `404` (not found), `400` (validation) |

### `GET /api/intake/form-schemas`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeRead` |
| Success | `200 OK` with `FormSchemaSummaryResponse[]` |

### `GET /api/intake/form-schemas/{id}`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeRead` |
| Success | `200 OK` with `FormSchemaResponse` |

### `POST /api/intake/form-schemas/{id}/qr-link`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `QRGenerate` |
| Request | `{ expiryHours }` (1-8760) |
| Success | `200 OK` with `GenerateIntakeQrLinkResponse` |
| Failure | `400` (not published), `404` (not found) |

### `GET /api/intake/submissions?status={n}`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeReview` |
| Query | `status` (optional, 0-6) |
| Success | `200 OK` with `PreVisitIntakeResponse[]` |

### `GET /api/intake/submissions/{id}`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeReview` |
| Success | `200 OK` with `PreVisitIntakeDetailsResponse` |
| Failure | `404` (not found) |

### `PATCH /api/intake/submissions/{id}/status`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeReview` |
| Request | `{ newStatus: IntakeStatus }` (0-6) |
| Success | `200 OK` with updated `PreVisitIntakeResponse` |
| Failure | `404` (not found), `400` (invalid status) |

### `POST /api/intake/submissions/{id}/convert-to-patient`
| Aspect | Value |
|---|---|
| Auth | `Bearer <jwt>` |
| Permission | `IntakeConvert` |
| Success | `503 Service Unavailable` (stubbed, expected) |
| Failure | `404` (not found), `400` (already converted) |

## Public Endpoints

### `GET /api/public/intake/{token}`
| Aspect | Value |
|---|---|
| Auth | None |
| Success | `200 OK` with `PublicIntakeFormResponse` |
| Failure | `400` (invalid/expired token) |

### `POST /api/public/intake/{token}/submit`
| Aspect | Value |
|---|---|
| Auth | None |
| Request | `{ patientName, patientEmail?, patientPhone?, formSubmissionData, painPointsData? }` |
| Success | `201 Created` with `PublicIntakeSubmissionResponse` |
| Failure | `400` (validation, invalid token, invalid submission) |

---

# 12. Final Release Checklist

> **Instructions:** Print this page. Check each box after verifying the corresponding test.

## Authentication

- [ ] Admin user is seeded on first startup (`Admin@PhysioAssist.com` / `P@ssord123`)
- [ ] New doctor registration returns `201` with JWT
- [ ] Registration with duplicate email returns `409`
- [ ] Registration with weak password returns `400`
- [ ] Email confirmation OTP is sent via Hangfire
- [ ] Hangfire dashboard accessible at `/jobs` (Admin/iti)
- [ ] Confirm email with valid OTP succeeds
- [ ] Confirm email with wrong OTP returns error
- [ ] Login with valid credentials returns `200` with tokens
- [ ] Login with wrong password returns `401`
- [ ] Login with unconfirmed email returns `401`
- [ ] Refresh token endpoint returns new tokens
- [ ] Reused refresh token returns error (rotation)
- [ ] Password reset flow works end-to-end
- [ ] JWT expiry of 30 minutes is enforced

## Intake — Form Schema Management

- [ ] Schema list page loads at `/intake/schemas`
- [ ] Search filters schemas by name (client-side)
- [ ] Paginator works with many schemas
- [ ] "Create New Schema" navigates to `/intake/schemas/new`
- [ ] Schema builder loads with empty form
- [ ] Can add sections, groups, questions via tree
- [ ] All 12 question types can be added
- [ ] Validation rules can be set on questions
- [ ] AND/OR conditions can be configured
- [ ] Live preview updates as changes are made
- [ ] Save as Draft stores in DB
- [ ] Edit existing Draft loads saved data
- [ ] Schema name uniqueness enforced per doctor
- [ ] Publish Draft → status changes to "Published"
- [ ] Publish already published schema is idempotent
- [ ] Edit Published schema is rejected
- [ ] QR generation requires Published status
- [ ] QR dialog accepts expiry hours (1–8760)
- [ ] QR "Copy" copies URL to clipboard
- [ ] QR "Open" opens URL in new tab

## Intake — Public Form

- [ ] Public intake URL loads form schema
- [ ] Invalid/expired token shows error message
- [ ] Patient name is required
- [ ] Patient email/phone are optional
- [ ] All question types render correctly
- [ ] Conditional questions show/hide based on answers
- [ ] Required field validation works client-side
- [ ] Body diagram pain points work (click, intensity, remove)
- [ ] Submission success shows confirmation with ID
- [ ] Submission with invalid data is rejected
- [ ] Duplicate submission creates new record
- [ ] Submission creates PreVisitIntake with `Status=Pending`

## Intake — Submission Review

- [ ] Submission list loads at `/intake/submissions`
- [ ] Search filters by patient name or email
- [ ] Status filter sends correct backend enum values
- [ ] All 7 statuses display correct label and color
- [ ] Row click navigates to detail
- [ ] Detail page shows patient info
- [ ] Detail page shows form metadata
- [ ] Detail page shows parsed answers with question text
- [ ] Pain points display on body SVG (if applicable)
- [ ] Pending submission shows "Approve" button
- [ ] Approve → confirmation dialog → status changes to "Approved"
- [ ] Approved submission shows "Convert to Patient" + "Reject" buttons
- [ ] Reject → confirmation → status changes to "Rejected" (terminal)
- [ ] Convert → dialog opens with patient summary
- [ ] Convert confirm → expected 503 (stubbed service)
- [ ] Terminal states (Rejected, Converted, Expired) show no actions
- [ ] Refresh button reloads data
- [ ] Go Back returns to list
- [ ] Error state shows retry button

## UI/UX

- [ ] Loading spinner displays during API calls
- [ ] Empty state shown when no data exists
- [ ] Error state with retry shown on failure
- [ ] Toast notifications appear for success/error
- [ ] 404 page for invalid routes
- [ ] Responsive layout at 375px, 768px, 1200px widths
- [ ] Accessible: ARIA labels on inputs, buttons, dialogs
- [ ] Status chips have distinct colors
- [ ] All buttons have loading state
- [ ] Confirmation dialogs prevent accidental actions

## API Security

- [ ] Intake endpoints require `Bearer` token
- [ ] Public endpoints do not require auth
- [ ] Custom permission attributes enforced (`IntakeRead`, `IntakeReview`, etc.)
- [ ] Auth with missing permission returns `403`
- [ ] Auth with expired token returns `401`
- [ ] CORS restricts to allowed origins
- [ ] QR tokens use HMAC-SHA256 signing
- [ ] QR tokens validate expiry

## Database

- [ ] Seed data creates Admin + SoloDoctor roles
- [ ] Seed data creates admin user with all permissions
- [ ] Migrations apply automatically on first run
- [ ] All 3 migrations applied (`__EFMigrationsHistory` has 3 rows)
- [ ] Hangfire tables created in DB
- [ ] Unique constraint on `intake.PatientFormSchemas(DoctorId, Name)`

## Build

- [ ] Backend builds with `dotnet build` — zero errors
- [ ] Frontend builds with `ng build --configuration production` — zero errors
- [ ] Angular strict mode passes
- [ ] No TypeScript errors in production build
- [ ] No duplicate service/model/renderer issues

---

## Known Limitations (Not Bugs)

| Item | Description |
|---|---|
| Convert to Patient | Always returns `503 ConversionDependencyMissing` — Patient module not integrated |
| Scheduling | Entity only (no API or UI) |
| Sessions | Entity only (no API or UI) |
| Patient Records | Entity only (no API or UI) |
| Reports | Entity only (no API or UI) |
| File Upload | Disabled in form renderer |
| Notifications | Entity only (no API or UI) |
| AI Chat/Summary/Extraction/Scheduler | Not implemented |
| Search/Semantic Search | Not implemented |
| Payments | Not implemented |
| Administration | Not implemented |
| Email sending | Uses Ethereal (development-only SMTP) — emails not actually delivered |
| Image upload | Only during registration — Cloudinary configured but needs valid API key |
| Audio transcription | Infrastructure exists (Groq + Gemini) but no frontend UI |
| Optimistic concurrency | Not implemented — last write wins on schema edits |
| Idempotency | Not implemented — duplicate submissions possible |
| Auth guards | Frontend has no auth guard — all pages are accessible without login (API still enforces JWT) |
