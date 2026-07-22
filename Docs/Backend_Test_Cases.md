# PhysioAssist - Complete Backend Test Cases

> **Project:** PhysioAssist Backend API (.NET 10)
> **Base URL:** `https://localhost:7097/api`
> **Auth:** JWT Bearer + Refresh Tokens
> **Database:** SQL Server (LocalDB)
> **Seed Data:** `Admin@PhysioAssist.com` / `P@ssord123`

---

## Table of Contents

1. Authentication Module (1.1 - 1.8)
2. Intake - Form Schema Management (2.1 - 2.7)
3. Intake - Public Access (3.1 - 3.2)
4. Intake - Submission Review (4.1 - 4.4)
5. Complete End-to-End Flows
6. Negative Test Cases
7. Edge Cases
8. Database Verification Queries

---

# 1. Authentication Module

---

## 1.1 POST /api/Auth/registration

### Purpose
Register a new doctor account. Creates user, doctor profile, OTP, and enqueues confirmation email.

### Request
```
POST https://localhost:7097/api/Auth/registration
Content-Type: multipart/form-data
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Email | string | Yes | Doctor email |
| Password | string | Yes | 8+ chars, uppercase, lowercase, digit, special char |
| FirstName | string | Yes | 3-100 chars |
| LastName | string | Yes | 3-100 chars |
| UserName | string | Yes | 3-100 chars, alphanumeric+underscore only |
| ClinicName | string | Yes | Clinic name |
| ProfilePhoto | file | No | JPG/PNG, max 2MB |

### Success Response: 201 Created
```json
{
  "id": "guid",
  "firstName": "John",
  "lastName": "Smith",
  "email": "doctor@example.com",
  "userName": "drjohn",
  "token": "jwt-string",
  "expiresIn": 1800,
  "refreshToken": "base64-string",
  "refreshTokenExpiryDate": "2026-10-02T...",
  "profilePictureUrl": "https://res.cloudinary.com/..."
}
```

### Backend Behavior
1. Creates ApplicationUser in auth.AspNetUsers (EmailConfirmed=false)
2. Uploads profile photo to Cloudinary (if provided)
3. Creates Doctor record in auth.Doctors
4. Creates OtpEntry in auth.OtpEntries (Purpose=EmailVerification, 15min expiry)
5. Enqueues email via Hangfire
6. Adds user to SoloDoctor role

### Database Changes
| Table | Operation |
|-------|-----------|
| auth.AspNetUsers | INSERT |
| auth.Doctors | INSERT |
| auth.OtpEntries | INSERT |
| auth.AspNetUserRoles | INSERT (SoloDoctor) |
| Hangfire tables | INSERT background job |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-R01 | Valid registration without photo | All fields valid, no ProfilePhoto | Success, AuthResponse returned | 201 |
| TC-R02 | Valid registration with photo | All fields valid + JPG file | Success, profilePictureUrl not null | 201 |
| TC-R03 | Duplicate email | Email already exists | User.DuplicatedEmail | 409 |
| TC-R04 | Weak password (no uppercase) | password123! | Password validation error | 400 |
| TC-R05 | Weak password (no digit) | Password! | Password validation error | 400 |
| TC-R06 | Weak password (no special char) | Password1 | Password validation error | 400 |
| TC-R07 | Weak password (too short) | Pass1! | Password validation error | 400 |
| TC-R08 | Empty email | "" | Email required validation | 400 |
| TC-R09 | Invalid email format | "notanemail" | Invalid email validation | 400 |
| TC-R10 | Empty password | "" | Password required validation | 400 |
| TC-R11 | Empty FirstName | "" | First name required validation | 400 |
| TC-R12 | FirstName too short | "Jo" | First name length validation | 400 |
| TC-R13 | Empty LastName | "" | Last name required validation | 400 |
| TC-R14 | Empty UserName | "" | Username required validation | 400 |
| TC-R15 | UserName with special chars | "dr@john" | Username format validation | 400 |
| TC-R16 | UserName too short | "dr" | Username length validation | 400 |
| TC-R17 | Invalid file type (PDF) | PDF file as ProfilePhoto | Image type validation error | 400 |
| TC-R18 | Oversized photo (> 2MB) | Large JPG file | Image size validation error | 400 |
| TC-R19 | All empty fields | All "" | Multiple validation errors | 400 |
| TC-R20 | Valid registration (concurrent) | Two registrations same time | Both succeed if different emails | 201 |

---

## 1.2 POST /api/Auth/confirm-email

### Purpose
Confirm user email using 6-digit OTP sent via Hangfire/Ethereal.

### Request
```
POST https://localhost:7097/api/Auth/confirm-email
Content-Type: application/json

{
  "email": "doctor@example.com",
  "code": "123456"
}
```

### Success Response: 204 No Content

### Backend Behavior
1. Finds user by email
2. Checks email not already confirmed
3. Finds latest unused OTP for EmailVerification purpose
4. Validates OTP not expired (15 min window)
5. Compares OTP code
6. Marks OTP as used
7. Sets EmailConfirmed=true
8. Adds user to SoloDoctor role

### Database Changes
| Table | Operation |
|-------|-----------|
| auth.AspNetUsers | UPDATE EmailConfirmed=true |
| auth.OtpEntries | UPDATE IsUsed=true |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-CE01 | Valid confirmation | Correct email + correct OTP | Success | 204 |
| TC-CE02 | Wrong OTP code | Correct email + wrong OTP | User.InvalidCode | 400 |
| TC-CE03 | Expired OTP | Correct email + correct OTP after 15min | User.InvalidCode | 400 |
| TC-CE04 | Non-existent user | "noone@example.com" + any code | User.InvalidCode | 400 |
| TC-CE05 | Already confirmed email | Confirm twice | User.DuplicatedConfirmation | 409 |
| TC-CE06 | Invalid email format | "notanemail" + code | Email validation error | 400 |
| TC-CE07 | Empty email | "" + code | Email required validation | 400 |
| TC-CE08 | Empty code | email + "" | OTP validation error | 400 |
| TC-CE09 | OTP not 6 digits | email + "12345" | OTP length validation | 400 |
| TC-CE10 | OTP not numeric | email + "abcdef" | OTP pattern validation | 400 |
| TC-CE11 | OTP out of range | email + "000001" | OTP range validation | 400 |

---

## 1.3 POST /api/Auth/resend-confirmation-email

### Purpose
Resend OTP for email confirmation. Invalidates old OTPs.

### Request
```
POST https://localhost:7097/api/Auth/resend-confirmation-email
Content-Type: application/json

{
  "email": "doctor@example.com"
}
```

### Success Response: 204 No Content

### Backend Behavior
1. If user not found -> returns success (prevents email enumeration)
2. If email already confirmed -> DuplicatedConfirmation
3. Invalidates all unused EmailVerification OTPs
4. Creates new OTP
5. Enqueues new email via Hangfire

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-RE01 | Valid resend (unconfirmed) | Existing unconfirmed email | Success | 204 |
| TC-RE02 | Non-existent email | "noone@example.com" | Success (prevents enumeration) | 204 |
| TC-RE03 | Already confirmed email | Confirmed email | User.DuplicatedConfirmation | 409 |
| TC-RE04 | Invalid email format | "notanemail" | Email validation error | 400 |
| TC-RE05 | Empty email | "" | Email required validation | 400 |
| TC-RE06 | Old OTP invalidated | Resend then use old OTP | Old OTP fails | 400 |

---

## 1.4 POST /api/Auth/login

### Purpose
Authenticate user and receive JWT + refresh token.

### Request
```
POST https://localhost:7097/api/Auth/login
Content-Type: application/json

{
  "email": "Admin@PhysioAssist.com",
  "password": "P@ssord123"
}
```

### Success Response: 200 OK
```json
{
  "id": "guid",
  "firstName": "Admin",
  "lastName": "User",
  "email": "Admin@PhysioAssist.com",
  "userName": "admin",
  "token": "jwt-string",
  "expiresIn": 1800,
  "refreshToken": "base64-string",
  "refreshTokenExpiryDate": "2026-10-02T...",
  "profilePictureUrl": null
}
```

### Backend Behavior
1. Finds user by email
2. Checks IsDisabled=false
3. Calls PasswordSignInAsync
4. If IsNotAllowed -> EmailNotConfirmed
5. If IsLockedOut -> LockedUser
6. If wrong password -> InvalidCredentials
7. Generates JWT with claims: sub, email, given_name, family_name, jti, Roles, Permissions
8. Generates refresh token (90-day expiry)
9. Saves refresh token to DB

### Database Changes
| Table | Operation |
|-------|-----------|
| auth.RefreshTokens | INSERT |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-L01 | Valid login (admin) | Admin credentials | Success + AuthResponse | 200 |
| TC-L02 | Valid login (new doctor) | Newly registered + confirmed | Success + AuthResponse | 200 |
| TC-L03 | Wrong password | Correct email, wrong password | User.InvalidCredentials | 401 |
| TC-L04 | Non-existent email | "noone@example.com" + any | User.InvalidCredentials | 401 |
| TC-L05 | Unconfirmed email | Register but dont confirm | User.EmailNotConfirmed | 401 |
| TC-L06 | Disabled user | User with IsDisabled=true | User.DisabledUser | 403 |
| TC-L07 | Locked out user | Multiple failed attempts | User.LockedUser | 403 |
| TC-L08 | Empty email | "" + password | Email required validation | 400 |
| TC-L09 | Empty password | email + "" | Password required validation | 400 |
| TC-L10 | Invalid email format | "notanemail" + password | Email validation error | 400 |
| TC-L11 | Weak password format | email + "123" | Password format validation | 400 |
| TC-L12 | JWT token validity | Check token claims | Contains sub, email, roles, permissions | 200 |
| TC-L13 | Refresh token in response | Check response fields | refreshToken not null, expiry 90 days | 200 |
| TC-L14 | Account lockout after 5 failures | 5 wrong password attempts | 6th returns LockedUser | 403 |

---

## 1.5 POST /api/Auth/new-refresh

### Purpose
Get new JWT + refresh token pair. Implements refresh token rotation.

### Request
```
POST https://localhost:7097/api/Auth/new-refresh
Content-Type: application/json

{
  "token": "current-jwt-token",
  "refreshToken": "current-refresh-token"
}
```

### Success Response: 200 OK
```json
{
  "id": "guid",
  "firstName": "...",
  "lastName": "...",
  "email": "...",
  "userName": "...",
  "token": "new-jwt-token",
  "expiresIn": 1800,
  "refreshToken": "new-refresh-token",
  "refreshTokenExpiryDate": "...",
  "profilePictureUrl": null
}
```

### Backend Behavior
1. Validates JWT signature (ignores expiry)
2. Finds user by JWT sub claim
3. Checks user not disabled, not locked out
4. Finds active refresh token
5. Revokes old refresh token
6. Generates new JWT + refresh token
7. Saves to DB

### Database Changes
| Table | Operation |
|-------|-----------|
| auth.RefreshTokens | UPDATE old token RevokedOn |
| auth.RefreshTokens | INSERT new token |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-RT01 | Valid refresh | Valid JWT + active refresh token | Success + new tokens | 200 |
| TC-RT02 | Reuse revoked refresh token | Old refresh token after rotation | User.InvalidRefreshToken | 401 |
| TC-RT03 | Invalid JWT (tampered) | Modified JWT payload | User.InvalidJwtToken | 401 |
| TC-RT04 | Invalid JWT (random string) | "invalid-jwt" | User.InvalidJwtToken | 401 |
| TC-RT05 | Empty token fields | "" + "" | Validation error | 400 |
| TC-RT06 | Empty refreshToken | valid JWT + "" | Validation error | 400 |
| TC-RT07 | Disabled user refresh | JWT of disabled user | User.DisabledUser | 401 |
| TC-RT08 | Expired JWT (valid signature) | Expired but valid JWT | Works (ignores expiry) | 200 |
| TC-RT09 | Refresh token not in DB | Valid JWT + random refresh string | User.InvalidRefreshToken | 401 |
| TC-RT10 | Locked out user refresh | JWT of locked user | User.LockedUser | 401 |

---

## 1.6 POST /api/Auth/revoke-refresh-token

### Purpose
Revoke a specific refresh token (logout).

### Request
```
POST https://localhost:7097/api/Auth/revoke-refresh-token
Content-Type: application/json

{
  "token": "current-jwt-token",
  "refreshToken": "current-refresh-token"
}
```

### Success Response: 204 No Content

### Backend Behavior
1. Validates JWT signature (ignores expiry)
2. Finds user by JWT sub claim
3. Checks user not disabled, not locked out
4. Finds active refresh token
5. Sets RevokedOn=now
6. Saves to DB

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-RR01 | Valid revoke | Valid JWT + active refresh token | Success | 204 |
| TC-RR02 | Revoke already revoked token | Same token twice | User.InvalidRefreshToken | 401 |
| TC-RR03 | Invalid JWT | Tampered JWT | User.InvalidJwtToken | 401 |
| TC-RR04 | Non-existent refresh token | Valid JWT + random string | User.InvalidRefreshToken | 401 |
| TC-RR05 | Empty fields | "" + "" | Validation error | 400 |
| TC-RR06 | Disabled user revoke | JWT of disabled user | User.DisabledUser | 401 |

---

## 1.7 POST /api/Auth/forget-passowrd

### Purpose
Send password reset OTP via email. Note: endpoint URL has typo "passowrd".

### Request
```
POST https://localhost:7097/api/Auth/forget-passowrd
Content-Type: application/json

{
  "email": "doctor@example.com"
}
```

### Success Response: 204 No Content

### Backend Behavior
1. If user not found -> returns success (prevents email enumeration)
2. Creates OTP with Purpose=PasswordReset, 15min expiry
3. Enqueues email via Hangfire

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-FP01 | Valid forget password | Existing email | Success | 204 |
| TC-FP02 | Non-existent email | "noone@example.com" | Success (prevents enumeration) | 204 |
| TC-FP03 | Invalid email format | "notanemail" | Email validation error | 400 |
| TC-FP04 | Empty email | "" | Email required validation | 400 |
| TC-FP05 | Multiple requests | Same email twice | Both succeed, new OTP each time | 204 |

---

## 1.8 POST /api/Auth/reset-password

### Purpose
Reset password using OTP from forget-password flow.

### Request
```
POST https://localhost:7097/api/Auth/reset-password
Content-Type: application/json

{
  "email": "doctor@example.com",
  "newPassword": "NewP@ssword1",
  "otp": "123456"
}
```

### Success Response: 204 No Content

### Backend Behavior
1. Finds user by email
2. Finds latest unused PasswordReset OTP
3. Validates OTP not expired (15 min window)
4. Compares OTP code
5. Generates password reset token
6. Resets password
7. Marks OTP as used

### Database Changes
| Table | Operation |
|-------|-----------|
| auth.AspNetUsers | UPDATE password hash |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-RPW01 | Valid reset | Correct email + OTP + new strong password | Success | 204 |
| TC-RPW02 | Wrong OTP | Correct email + wrong OTP + new password | User.InvalidCode | 400 |
| TC-RPW03 | Expired OTP | Correct email + expired OTP | User.InvalidCode | 400 |
| TC-RPW04 | Non-existent user | "noone@example.com" + OTP + password | User.InvalidCode | 400 |
| TC-RPW05 | Weak new password | Correct email + OTP + "123" | Password validation error | 400 |
| TC-RPW06 | Invalid email format | "notanemail" + OTP + password | Email validation error | 400 |
| TC-RPW07 | Empty email | "" + OTP + password | Email required validation | 400 |
| TC-RPW08 | Empty OTP | email + "" + password | OTP validation error | 400 |
| TC-RPW09 | Empty new password | email + OTP + "" | Password required validation | 400 |
| TC-RPW10 | OTP not numeric | email + "abcdef" + password | OTP pattern validation | 400 |
| TC-RPW11 | Login with old password after reset | Reset password, try old password | InvalidCredentials | 401 |
| TC-RPW12 | Login with new password after reset | Reset password, login with new | Success | 200 |
| TC-RPW13 | Reuse OTP after reset | Reset, try same OTP again | InvalidCode | 400 |

---

# 2. Intake - Form Schema Management

---

## 2.1 POST /api/intake/form-schemas

### Purpose
Create a new form schema for patient intake.

### Request
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

### Schema JSON Structure
```json
{
  "schemaVersion": 1,
  "sections": [
    {
      "sectionId": "section-1",
      "title": "Patient Info",
      "order": 1,
      "groups": [
        {
          "groupId": "group-1",
          "title": "Personal Details",
          "order": 1,
          "questions": [
            {
              "questionId": "q-name",
              "text": "Full Name",
              "type": "text",
              "required": true,
              "order": 1,
              "validationRules": [
                { "ruleType": "minLength", "value": "2", "message": "Name too short" }
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

### Supported Question Types
text, textarea, number, email, phone, date, select, multiselect, radio, checkbox, file, painpoint

### Supported Validation Rule Types
required, minLength, maxLength, min, max, pattern, email, url

### Supported Condition Operators
equals, notEquals, contains, greaterThan, lessThan, in, notIn

### Success Response: 200 OK
```json
{
  "id": "guid",
  "name": "Patient Intake Form",
  "description": "Standard intake questionnaire",
  "schemaJson": "...",
  "doctorId": "guid",
  "version": 1,
  "status": 0,
  "isDefault": false,
  "schemaHash": "base64-hash",
  "createdAt": "2026-07-04T...",
  "updatedAt": null
}
```

### Backend Behavior
1. Deserializes SchemaJson into DynamicFormSchemaDto
2. Validates schema: unique IDs, valid question types, valid validation rules, valid condition operators
3. Checks for duplicate name per doctor
4. If isDefault=true, unsets other defaults for this doctor
5. Computes SHA-256 hash of schema JSON
6. Creates PatientFormSchema with Status=Draft, Version=1

### Database Changes
| Table | Operation |
|-------|-----------|
| intake.PatientFormSchemas | INSERT |
| intake.PatientFormSchemas | UPDATE IsDefault=false for others (if new default) |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-CS01 | Valid create (minimal) | Name + valid SchemaJson | Success | 200 |
| TC-CS02 | Valid create with description | Name + description + SchemaJson | Success | 200 |
| TC-CS03 | Valid create as default | isDefault=true | Success, other schemas unset default | 200 |
| TC-CS04 | Empty name | name="" | Schema name required | 400 |
| TC-CS05 | Name exceeds 150 chars | 151-char name | MaxLength validation | 400 |
| TC-CS06 | Empty SchemaJson | schemaJson="" | Schema JSON required | 400 |
| TC-CS07 | Invalid JSON SchemaJson | "not-json" | InvalidSchema | 400 |
| TC-CS08 | Duplicate name per doctor | Same name as existing schema | SchemaNameDuplicated | 409 |
| TC-CS09 | Same name, different doctors | Two doctors create same name | Both succeed | 200 |
| TC-CS10 | Schema with duplicate section IDs | Two sections same sectionId | InvalidSchema | 400 |
| TC-CS11 | Schema with duplicate question IDs | Two questions same questionId | InvalidSchema | 400 |
| TC-CS12 | Schema with invalid question type | type="unknown" | InvalidSchema | 400 |
| TC-CS13 | Schema with invalid validation rule | ruleType="unknown" | InvalidSchema | 400 |
| TC-CS14 | Schema with invalid condition operator | operator="unknown" | InvalidSchema | 400 |
| TC-CS15 | Schema with empty section ID | sectionId="" | InvalidSchema | 400 |
| TC-CS16 | Schema with empty question ID | questionId="" | InvalidSchema | 400 |
| TC-CS17 | Schema with no sections | sections=[] | InvalidSchema | 400 |
| TC-CS18 | Schema with section, no groups | groups=[] | InvalidSchema | 400 |
| TC-CS19 | Schema with group, no questions | questions=[] | InvalidSchema | 400 |
| TC-CS20 | Schema with condition targeting non-existent question | TargetQuestionId="fake" | InvalidSchema | 400 |
| TC-CS21 | Schema with zero SchemaVersion | schemaVersion=0 | InvalidSchema | 400 |
| TC-CS22 | Schema with multiple sections | 3 valid sections | Success | 200 |
| TC-CS23 | Schema with all question types | One of each supported type | Success | 200 |
| TC-CS24 | Schema with conditional logic | Question with equals condition | Success | 200 |
| TC-CS25 | Unauthenticated request | No Bearer token | Unauthorized | 401 |
| TC-CS26 | User without IntakeManageForms permission | JWT without permission | Forbidden | 403 |
| TC-CS27 | Description exceeds 500 chars | 501-char description | MaxLength validation | 400 |
| TC-CS28 | Description exactly 500 chars | 500-char description | Success | 200 |

---

## 2.2 PUT /api/intake/form-schemas/{id}

### Purpose
Update an existing form schema. Increments version.

### Request
```
PUT https://localhost:7097/api/intake/form-schemas/{schemaId}
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "name": "Updated Form Name",
  "description": "Updated description",
  "schemaJson": "{...updated-schema...}",
  "isDefault": false
}
```

### Success Response: 200 OK
Returns FormSchemaResponse with incremented version.

### Backend Behavior
1. Finds schema by ID
2. Validates doctor ownership
3. Deserializes and validates new schema JSON
4. Checks name uniqueness (excluding current schema)
5. Updates fields, increments version
6. Handles default toggle logic

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-US01 | Valid update (Draft schema) | Valid data for Draft schema | Success, version incremented | 200 |
| TC-US02 | Update name only | Same SchemaJson, new name | Success, version incremented | 200 |
| TC-US03 | Update description only | Same SchemaJson, new description | Success | 200 |
| TC-US04 | Set as default | isDefault=true on non-default | Success, other schemas unset | 200 |
| TC-US05 | Unset default | isDefault=false on default | Success, schema no longer default | 200 |
| TC-US06 | Non-existent schema | Random GUID | SchemaNotFound | 404 |
| TC-US07 | Update other doctors schema | Different doctorId | UnauthorizedDoctor | 403 |
| TC-US08 | Duplicate name conflict | Name matches another schema | SchemaNameDuplicated | 409 |
| TC-US09 | Empty name | name="" | Schema name required | 400 |
| TC-US10 | Invalid SchemaJson | "not-json" | InvalidSchema | 400 |
| TC-US11 | Empty SchemaJson | schemaJson="" | Schema JSON required | 400 |
| TC-US12 | Unauthenticated | No token | Unauthorized | 401 |
| TC-US13 | Version increments correctly | Update twice | Version goes 1->2->3 | 200 |
| TC-US14 | SchemaHash recalculated | Update with different SchemaJson | SchemaHash changes | 200 |
| TC-US15 | Update with valid conditions | Condition targeting existing question | Success | 200 |

---

## 2.3 POST /api/intake/form-schemas/{id}/publish

### Purpose
Publish a Draft schema to make it available for QR generation and public intake.

### Request
```
POST https://localhost:7097/api/intake/form-schemas/{schemaId}/publish
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "version": 1
}
```

### Success Response: 200 OK
Returns FormSchemaResponse with Status=Published.

### Backend Behavior
1. Finds schema by ID
2. Validates doctor ownership
3. If already Published -> returns existing (idempotent)
4. Sets Status=Published, PublishedAt=now, increments version

### Database Changes
| Table | Operation |
|-------|-----------|
| intake.PatientFormSchemas | UPDATE Status=1, PublishedAt=now, Version+=1 |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-PB01 | Publish Draft schema | Valid Draft schema ID | Success, Status=Published | 200 |
| TC-PB02 | Publish already published (idempotent) | Published schema ID | Success, no changes | 200 |
| TC-PB03 | Non-existent schema | Random GUID | SchemaNotFound | 404 |
| TC-PB04 | Publish other doctor's schema | Different doctorId | UnauthorizedDoctor | 403 |
| TC-PB05 | Version increments on publish | Publish Draft | Version += 1 | 200 |
| TC-PB06 | PublishedAt is set | Publish Draft | PublishedAt not null | 200 |
| TC-PB07 | Unauthenticated | No token | Unauthorized | 401 |
| TC-PB08 | Version=0 in request body | version=0 | Version validation error | 400 |
| TC-PB09 | Publish then generate QR | Publish -> QR link | QR link generated | 200 |

---

## 2.4 GET /api/intake/form-schemas

### Purpose
List all form schemas for the authenticated doctor.

### Request
```
GET https://localhost:7097/api/intake/form-schemas
Authorization: Bearer <jwt>
```

### Success Response: 200 OK
```json
[
  {
    "id": "guid",
    "name": "Patient Intake Form",
    "description": "...",
    "version": 1,
    "status": 0,
    "isDefault": false,
    "publishedAt": null,
    "createdAt": "2026-07-04T..."
  }
]
```

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-LS01 | List with no schemas | New doctor, no schemas | Empty array | 200 |
| TC-LS02 | List with one schema | Doctor has 1 schema | Array of 1 | 200 |
| TC-LS03 | List with multiple schemas | Doctor has 3 schemas | Array of 3 | 200 |
| TC-LS04 | Only own schemas visible | Two doctors, each with schemas | Each sees only their schemas | 200 |
| TC-LS05 | Unauthenticated | No token | Unauthorized | 401 |
| TC-LS06 | User without IntakeRead permission | JWT without permission | Forbidden | 403 |
| TC-LS07 | Schemas include Draft and Published | Mix of statuses | All returned with correct status | 200 |

---

## 2.5 GET /api/intake/form-schemas/{id}

### Purpose
Get a specific form schema by ID.

### Request
```
GET https://localhost:7097/api/intake/form-schemas/{schemaId}
Authorization: Bearer <jwt>
```

### Success Response: 200 OK
Returns full FormSchemaResponse including SchemaJson.

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-GS01 | Get existing schema | Valid schema ID | Success, full schema | 200 |
| TC-GS02 | Non-existent schema | Random GUID | SchemaNotFound | 404 |
| TC-GS03 | Get other doctor's schema | Different doctorId | UnauthorizedDoctor | 403 |
| TC-GS04 | Get Draft schema | Draft schema ID | Success | 200 |
| TC-GS05 | Get Published schema | Published schema ID | Success | 200 |
| TC-GS06 | Unauthenticated | No token | Unauthorized | 401 |

---

## 2.6 GET /api/intake/form-schemas/default

### Purpose
Get the default form schema for the authenticated doctor.

### Request
```
GET https://localhost:7097/api/intake/form-schemas/default
Authorization: Bearer <jwt>
```

### Success Response: 200 OK
Returns FormSchemaResponse for the default schema.

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-DS01 | Get default schema | Doctor has a default | Success | 200 |
| TC-DS02 | No default schema set | Doctor has schemas but none default | SchemaNotFound | 404 |
| TC-DS03 | No schemas at all | New doctor | SchemaNotFound | 404 |
| TC-DS04 | Unauthenticated | No token | Unauthorized | 401 |

---

## 2.7 POST /api/intake/form-schemas/{id}/qr-link

### Purpose
Generate a QR token for public intake access.

### Request
```
POST https://localhost:7097/api/intake/form-schemas/{schemaId}/qr-link
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "expiryHours": 24
}
```

### Success Response: 200 OK
```json
{
  "token": "base64url-hmac-token",
  "publicUrl": "base64url-hmac-token",
  "expiresAt": "2026-07-05T18:00:00Z"
}
```

### Backend Behavior
1. Validates schema is Published
2. Validates doctor ownership
3. Creates QRTokenPayload with Purpose=Intake, TargetId=schemaId, Expiry
4. HMAC-SHA256 signs the payload
5. Returns token + URL + expiry

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-QR01 | Valid QR generation | Published schema, expiryHours=24 | Success + token + URL | 200 |
| TC-QR02 | QR for Draft schema | Draft schema ID | SchemaNotFound (not published) | 404 |
| TC-QR03 | Non-existent schema | Random GUID | SchemaNotFound | 404 |
| TC-QR04 | Expiry = 0 hours | expiryHours=0 | Validation error (>0 required) | 400 |
| TC-QR05 | Expiry = 8761 hours | expiryHours=8761 | Validation error (max 8760) | 400 |
| TC-QR06 | Expiry = 8760 hours (max) | expiryHours=8760 | Success | 200 |
| TC-QR07 | Expiry = 1 hour (min) | expiryHours=1 | Success | 200 |
| TC-QR08 | QR for other doctor's schema | Different doctorId | UnauthorizedDoctor | 403 |
| TC-QR09 | Unauthenticated | No token | Unauthorized | 401 |
| TC-QR10 | Token is HMAC-SHA256 | Check token format | Base64URL encoded | 200 |
| TC-QR11 | ExpiresAt is correct | expiryHours=24 | ExpiresAt = now + 24h | 200 |

---

# 3. Intake - Public Access

---

## 3.1 GET /api/public/intake/{token}

### Purpose
Get form schema data for public intake (no auth required).

### Request
```
GET https://localhost:7097/api/public/intake/{token}
```

### Success Response: 200 OK
```json
{
  "formSchemaId": "guid",
  "formName": "Patient Intake Form",
  "formDescription": "Standard intake questionnaire",
  "schemaJson": "{...DynamicFormSchemaDto...}",
  "version": 1
}
```

### Backend Behavior
1. Validates QR token (HMAC signature + expiry + purpose=Intake)
2. Finds published schema by TargetId
3. Returns form data

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-PF01 | Valid token | Valid QR token | Success + form data | 200 |
| TC-PF02 | Expired token | Token past expiry | Invalid/expired token error | 400 |
| TC-PF03 | Tampered token | Modified token string | Invalid token error | 400 |
| TC-PF04 | Fake token | Random string | Invalid token error | 400 |
| TC-PF05 | Empty token | "" | Invalid token error | 400 |
| TC-PF06 | Token for deleted schema | Schema deleted from DB | SchemaNotFound | 404 |
| TC-PF07 | Token purpose mismatch | Token with Purpose=Patient | Invalid token error | 400 |
| TC-PF08 | Schema not published | Token for Draft schema | SchemaNotFound | 404 |

---

## 3.2 POST /api/public/intake/{token}/submit

### Purpose
Submit a patient intake form (no auth required).

### Request
```
POST https://localhost:7097/api/public/intake/{token}/submit
Content-Type: application/json

{
  "patientName": "Jane Doe",
  "patientEmail": "jane@example.com",
  "patientPhone": "+1234567890",
  "formSubmissionData": "{...DynamicFormSubmissionDto...}",
  "painPointsData": "[{\"bodyPart\":\"front\",\"x\":0.5,\"y\":0.3,\"intensity\":7}]"
}
```

### Submission JSON Structure
```json
{
  "schemaVersion": 1,
  "formSchemaId": "guid",
  "formSchemaVersion": 1,
  "sections": [
    {
      "sectionId": "section-1",
      "groups": [
        {
          "groupId": "group-1",
          "answers": [
            {
              "questionId": "q-name",
              "value": { "text": "Jane Doe" }
            }
          ]
        }
      ]
    }
  ]
}
```

### Success Response: 200 OK
```json
{
  "submissionId": "guid",
  "message": "Your intake form has been submitted successfully.",
  "submittedAt": "2026-07-04T18:00:00Z"
}
```

### Backend Behavior
1. Validates QR token
2. Finds published schema
3. Deserializes and validates submission against schema
4. Cross-references section/group/question IDs
5. Creates PreVisitIntake with Status=Pending
6. Stores token hash for audit

### Database Changes
| Table | Operation |
|-------|-----------|
| intake.PreVisitIntakes | INSERT (Status=Pending) |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-PS01 | Valid submission | All fields valid, correct token | Success + submissionId | 200 |
| TC-PS02 | Submission without email | patientEmail=null | Success | 200 |
| TC-PS03 | Submission without phone | patientPhone=null | Success | 200 |
| TC-PS04 | Submission without pain points | painPointsData=null | Success | 200 |
| TC-PS05 | Empty patient name | patientName="" | Patient name required | 400 |
| TC-PS06 | Patient name too long | 201-char name | MaxLength validation | 400 |
| TC-PS07 | Invalid patient email | "notanemail" | Email validation error | 400 |
| TC-PS08 | Patient email too long | 201-char email | MaxLength validation | 400 |
| TC-PS09 | Patient phone too long | 21-char phone | MaxLength validation | 400 |
| TC-PS10 | Empty formSubmissionData | formSubmissionData="" | Required validation | 400 |
| TC-PS11 | Invalid JSON submission | "not-json" | InvalidSubmission | 400 |
| TC-PS12 | Submission with wrong section ID | sectionId="fake" | InvalidSubmission | 400 |
| TC-PS13 | Submission with wrong group ID | groupId="fake" | InvalidSubmission | 400 |
| TC-PS14 | Submission with wrong question ID | questionId="fake" | InvalidSubmission | 400 |
| TC-PS15 | Expired token | Token past expiry | Invalid token error | 400 |
| TC-PS16 | Tampered token | Modified token | Invalid token error | 400 |
| TC-PS17 | Submission creates Pending status | Check DB | Status=0 (Pending) | 200 |
| TC-PS18 | Submission stores form data | Check DB | FormSubmissionData stored | 200 |
| TC-PS19 | Submission with pain points JSON | Valid painPointsData | Success, stored | 200 |
| TC-PS20 | Duplicate submission same token | Submit twice with same token | Both create new records | 200 |
| TC-PS21 | Submission schema version recorded | Check DB | FormSchemaVersion matches schema | 200 |

---

# 4. Intake - Submission Review

---

## 4.1 GET /api/intake/submissions

### Purpose
List all intake submissions for the authenticated doctor.

### Request
```
GET https://localhost:7097/api/intake/submissions
GET https://localhost:7097/api/intake/submissions?status=0
Authorization: Bearer <jwt>
```

### Query Parameters
| Parameter | Type | Required | Values |
|-----------|------|----------|--------|
| status | int? | No | 0=Pending, 1=Submitted, 2=InReview, 3=Approved, 4=Rejected, 5=Converted, 6=Expired |

### Success Response: 200 OK
```json
[
  {
    "id": "guid",
    "doctorId": "guid",
    "formSchemaId": "guid",
    "formSchemaVersion": 1,
    "patientName": "Jane Doe",
    "patientEmail": "jane@example.com",
    "patientPhone": "+1234567890",
    "status": 0,
    "convertedToPatientId": null,
    "submittedAt": "2026-07-04T18:00:00Z",
    "reviewedAt": null,
    "reviewedByDoctorId": null
  }
]
```

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-GS01 | List with no submissions | New doctor, no submissions | Empty array | 200 |
| TC-GS02 | List with one submission | Doctor has 1 submission | Array of 1 | 200 |
| TC-GS03 | Filter by status=0 (Pending) | ?status=0 | Only Pending submissions | 200 |
| TC-GS04 | Filter by status=3 (Approved) | ?status=3 | Only Approved submissions | 200 |
| TC-GS05 | Filter by non-existent status | ?status=99 | Validation error | 400 |
| TC-GS06 | Only own submissions visible | Two doctors, each with submissions | Each sees only theirs | 200 |
| TC-GS07 | Unauthenticated | No token | Unauthorized | 401 |
| TC-GS08 | User without IntakeReview permission | JWT without permission | Forbidden | 403 |
| TC-GS09 | All statuses returned correctly | Mix of statuses | Correct status values | 200 |
| TC-GS10 | No status filter (all) | No query param | All submissions returned | 200 |

---

## 4.2 GET /api/intake/submissions/{id}

### Purpose
Get detailed submission data including form answers and pain points.

### Request
```
GET https://localhost:7097/api/intake/submissions/{submissionId}
Authorization: Bearer <jwt>
```

### Success Response: 200 OK
```json
{
  "id": "guid",
  "doctorId": "guid",
  "formSchemaId": "guid",
  "formSchemaVersion": 1,
  "patientName": "Jane Doe",
  "patientEmail": "jane@example.com",
  "patientPhone": "+1234567890",
  "formSubmissionData": "{...}",
  "painPointsData": "[...]",
  "status": 0,
  "convertedToPatientId": null,
  "submittedAt": "2026-07-04T18:00:00Z",
  "reviewedAt": null,
  "reviewedByDoctorId": null,
  "formSchemaName": "Patient Intake Form"
}
```

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-GSD01 | Get existing submission | Valid submission ID | Success + full details | 200 |
| TC-GSD02 | Non-existent submission | Random GUID | SubmissionNotFound | 404 |
| TC-GSD03 | Get other doctor's submission | Different doctorId | UnauthorizedDoctor | 403 |
| TC-GSD04 | Submission includes formSubmissionData | Check response | formSubmissionData not null | 200 |
| TC-GSD05 | Submission includes painPointsData | Check response | painPointsData included if set | 200 |
| TC-GSD06 | Submission includes formSchemaName | Check response | formSchemaName matches schema | 200 |
| TC-GSD07 | Unauthenticated | No token | Unauthorized | 401 |

---

## 4.3 PATCH /api/intake/submissions/{id}/status

### Purpose
Update submission status (Approve, Reject, etc.).

### Request
```
PATCH https://localhost:7097/api/intake/submissions/{submissionId}/status
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "newStatus": 3
}
```

### IntakeStatus Enum Values
| Value | Name | Description |
|-------|------|-------------|
| 0 | Pending | Initial state after submission |
| 1 | Submitted | Submitted for review |
| 2 | InReview | Under review |
| 3 | Approved | Approved by doctor |
| 4 | Rejected | Rejected by doctor |
| 5 | Converted | Converted to patient |
| 6 | Expired | Link expired |

### Success Response: 200 OK
Returns updated PreVisitIntakeResponse.

### Backend Behavior
1. Finds submission by ID
2. Validates doctor ownership
3. Sets Status = newStatus
4. Sets ReviewedAt = now
5. Sets ReviewedByDoctorId = doctorId

### Database Changes
| Table | Operation |
|-------|-----------|
| intake.PreVisitIntakes | UPDATE Status, ReviewedAt, ReviewedByDoctorId |

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-US01 | Approve Pending submission | newStatus=3 on Pending | Success, status=Approved | 200 |
| TC-US02 | Reject Approved submission | newStatus=4 on Approved | Success, status=Rejected | 200 |
| TC-US03 | Set to InReview | newStatus=2 on Pending | Success, status=InReview | 200 |
| TC-US04 | Set to Submitted | newStatus=1 on Pending | Success | 200 |
| TC-US05 | Set to Expired | newStatus=6 on Pending | Success | 200 |
| TC-US06 | Invalid status value | newStatus=99 | IsInEnum validation error | 400 |
| TC-US07 | Non-existent submission | Random GUID | SubmissionNotFound | 404 |
| TC-US08 | Update other doctor's submission | Different doctorId | UnauthorizedDoctor | 403 |
| TC-US09 | ReviewedAt is set | Approve submission | ReviewedAt not null | 200 |
| TC-US10 | ReviewedByDoctorId is set | Approve submission | ReviewedByDoctorId matches doctor | 200 |
| TC-US11 | Unauthenticated | No token | Unauthorized | 401 |
| TC-US12 | Already terminal state | Reject already Rejected | Still succeeds (service allows) | 200 |
| TC-US13 | Approve then check list | Approve -> list with status=3 | Shows approved submission | 200 |

---

## 4.4 POST /api/intake/submissions/{id}/convert-to-patient

### Purpose
Convert an intake submission to a patient record. Currently STUBBED.

### Request
```
POST https://localhost:7097/api/intake/submissions/{submissionId}/convert-to-patient
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "notes": "Optional conversion notes"
}
```

### Success Response: 503 Service Unavailable (STUBBED)
```json
{
  "status": 503,
  "title": "Service Unavailable",
  "detail": "Patient conversion service is not available. Cannot convert intake to patient."
}
```

### Backend Behavior
1. Finds submission by ID
2. Validates doctor ownership
3. Checks not already converted
4. Returns ConversionDependencyMissing (503) - stubbed

### Test Cases

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-CTP01 | Convert Approved submission | Valid Approved submission | 503 ConversionDependencyMissing | 503 |
| TC-CTP02 | Convert non-existent submission | Random GUID | SubmissionNotFound | 404 |
| TC-CTP03 | Convert other doctor's submission | Different doctorId | UnauthorizedDoctor | 403 |
| TC-CTP04 | Convert already converted | Check AlreadyConverted | 503 (stub returns before check) | 503 |
| TC-CTP05 | Notes exceed 1000 chars | 1001-char notes | MaxLength validation | 400 |
| TC-CTP06 | Notes exactly 1000 chars | 1000-char notes | 503 (validation passes) | 503 |
| TC-CTP07 | Empty notes | notes=null | 503 (notes optional) | 503 |
| TC-CTP08 | Unauthenticated | No token | Unauthorized | 401 |

---

# 5. Complete End-to-End Flows

---

## Flow 1: Doctor Registration to First Patient Submission

### Prerequisites
- Clean database (fresh dotnet run)
- Ethereal email accessible

### Steps

| Step | Action | Endpoint | Expected |
|------|--------|----------|----------|
| 1 | Register new doctor | POST /api/Auth/registration | 201 + AuthResponse |
| 2 | Check Hangfire for email job | GET /jobs | Job completed |
| 3 | Get OTP from Ethereal email | - | 6-digit code visible |
| 4 | Confirm email with OTP | POST /api/Auth/confirm-email | 204 |
| 5 | Login | POST /api/Auth/login | 200 + JWT + refresh |
| 6 | Create form schema | POST /api/intake/form-schemas | 200 + schema |
| 7 | List schemas to verify | GET /api/intake/form-schemas | Array with new schema |
| 8 | Publish schema | POST /api/intake/form-schemas/{id}/publish | 200, Status=Published |
| 9 | Generate QR link | POST /api/intake/form-schemas/{id}/qr-link | 200 + token |
| 10 | Access public form | GET /api/public/intake/{token} | 200 + form data |
| 11 | Submit intake form | POST /api/public/intake/{token}/submit | 200 + submissionId |
| 12 | List submissions | GET /api/intake/submissions | Array with new submission |
| 13 | View submission detail | GET /api/intake/submissions/{id} | 200 + full details |
| 14 | Approve submission | PATCH /api/intake/submissions/{id}/status | 200, status=Approved |
| 15 | Attempt convert to patient | POST /api/intake/submissions/{id}/convert-to-patient | 503 (expected stub) |

---

## Flow 2: Refresh Token Rotation

| Step | Action | Expected |
|------|--------|----------|
| 1 | Login -> get JWT + refresh | 200 + tokens |
| 2 | Call /new-refresh with current tokens | 200 + NEW tokens |
| 3 | Try /new-refresh with OLD refresh token | 401 InvalidRefreshToken |
| 4 | Use NEW refresh token | 200 + newer tokens |
| 5 | Revoke newest refresh token | 204 |
| 6 | Try /new-refresh with revoked token | 401 InvalidRefreshToken |

---

## Flow 3: Password Reset

| Step | Action | Expected |
|------|--------|----------|
| 1 | POST /forget-passowrd with email | 204 |
| 2 | Get OTP from Ethereal | 6-digit code |
| 3 | POST /reset-password with email + OTP + new password | 204 |
| 4 | Login with OLD password | 401 InvalidCredentials |
| 5 | Login with NEW password | 200 + tokens |

---

## Flow 4: Schema Lifecycle (Draft -> Published -> QR -> Submit)

| Step | Action | Status/Version |
|------|--------|----------------|
| 1 | Create schema | Draft, v1 |
| 2 | Update schema | Draft, v2 |
| 3 | Update schema again | Draft, v3 |
| 4 | Publish schema | Published, v4 |
| 5 | Generate QR | Token valid |
| 6 | Patient submits via QR | Submission created |
| 7 | Doctor reviews submission | Pending status |
| 8 | Doctor approves | Approved status |
| 9 | Doctor rejects another | Rejected status (terminal) |

---

## Flow 5: Multiple Doctors Isolation

| Step | Action | Expected |
|------|--------|----------|
| 1 | Doctor A creates schema | Success |
| 2 | Doctor B creates same-named schema | Success (different doctor) |
| 3 | Doctor A lists schemas | Only Doctor A's schemas |
| 4 | Doctor B tries to update Doctor A's schema | 403 UnauthorizedDoctor |
| 5 | Doctor A generates QR for their schema | Success |
| 6 | Patient submits via QR | Submission linked to Doctor A |
| 7 | Doctor B lists submissions | Only Doctor B's submissions (empty) |
| 8 | Doctor A lists submissions | Shows patient submission |

---

# 6. Negative Test Cases

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 1 | Access protected endpoint without token | GET /api/intake/form-schemas without Bearer | 401 Unauthorized |
| 2 | Access with expired JWT | Wait 30+ min, use old token | 401 InvalidJwtToken |
| 3 | Access with tampered JWT | Modify JWT payload, send request | 401 InvalidJwtToken |
| 4 | Wrong permission for endpoint | User without IntakeManageForms tries POST /form-schemas | 403 Forbidden |
| 5 | Access other doctor's resource | Doctor B tries to get Doctor A's schema | 403 UnauthorizedDoctor |
| 6 | Submit with invalid QR token | Tamper with token, POST /submit | 400 Invalid token error |
| 7 | Submit with expired QR token | Use token past expiry | 400 Invalid token error |
| 8 | Submit with wrong schema structure | Submission section IDs dont match schema | 400 InvalidSubmission |
| 9 | Create schema with invalid JSON | schemaJson="not-json" | 400 InvalidSchema |
| 10 | Create schema with invalid question type | type="unknown" | 400 InvalidSchema |
| 11 | Update status with invalid enum value | newStatus=99 | 400 IsInEnum validation |
| 12 | Convert already converted submission | Try convert twice | 409 AlreadyConverted |
| 13 | Register with duplicate email | Same email as existing user | 409 DuplicatedEmail |
| 14 | Confirm email with wrong OTP | Incorrect code | 400 InvalidCode |
| 15 | Login with disabled account | IsDisabled=true user | 403 DisabledUser |
| 16 | Login with locked account | Multiple failed attempts | 403 LockedUser |
| 17 | Refresh with revoked token | Reuse old refresh token | 401 InvalidRefreshToken |
| 18 | Generate QR for Draft schema | Draft schema not published | 400 SchemaNotFound |
| 19 | Submit empty patient name | patientName="" | 400 validation error |
| 20 | Submit with invalid email format | patientEmail="notemail" | 400 validation error |

---

# 7. Edge Cases

| # | Edge Case | How to Test | Expected Result |
|---|-----------|-------------|-----------------|
| 1 | Very long patient name (200 chars) | Enter 200-char name | Success (max is 200) |
| 2 | Patient name exactly 201 chars | Enter 201-char name | 400 MaxLength validation |
| 3 | Description exactly 500 chars | 500-char description | Success |
| 4 | Description 501 chars | 501-char description | 400 MaxLength validation |
| 5 | Schema with 50+ sections | Create large schema | Success (no limit on sections) |
| 6 | Schema with 200+ questions | Create large schema | Success |
| 7 | QR token with 1 hour expiry | expiryHours=1 | Success, token valid for 1h |
| 8 | QR token with 8760 hour expiry | expiryHours=8760 | Success, token valid for 1 year |
| 9 | Concurrent schema edits | Two requests same schema | Last write wins |
| 10 | Submit twice rapidly | Click submit twice | Both create records (no idempotency) |
| 11 | Special characters in patient name | Name: "O'Brien-Smith" | Success, stored correctly |
| 12 | Unicode characters in name | Name with non-ASCII chars | Success |
| 13 | Empty optional fields | patientEmail=null, patientPhone=null | Success |
| 14 | Schema with all optional fields null | Only required fields set | Success |
| 15 | Token with maximum entropy | Very long token string | Invalid token error |
| 16 | Multiple default schema toggles | Create 3 schemas, toggle default | Only last one is default |
| 17 | Publish then unpublish | Publish schema, try to change status back | No unpublish endpoint (Status stays Published) |
| 18 | Get default when none set | Doctor with schemas but no default | 404 SchemaNotFound |
| 19 | Schema with conditional logic | Question hidden by condition | Only unconditional fields shown |
| 20 | Submission with empty sections array | sections=[] | 400 InvalidSubmission |

---

# 8. Database Verification Queries

### Verify User Registration
```sql
SELECT Id, Email, FirstName, LastName, EmailConfirmed, IsDisabled
FROM auth.AspNetUsers
WHERE Email = 'doctor@example.com';
```

### Verify OTP
```sql
SELECT Code, Purpose, ExpiresAt, IsUsed
FROM auth.OtpEntries
WHERE UserId = '<user-guid>';
```

### Verify Refresh Token
```sql
SELECT Token, ExpiresOn, RevokedOn
FROM auth.RefreshTokens
WHERE UserId = '<user-guid>' AND RevokedOn IS NULL;
```

### Verify Form Schema
```sql
SELECT Id, Name, Status, Version, IsDefault, PublishedAt, SchemaHash
FROM intake.PatientFormSchemas
WHERE DoctorId = '<doctor-guid>';
```

### Verify Submissions
```sql
SELECT Id, PatientName, Status, SubmittedAt, ReviewedAt, ReviewedByDoctorId
FROM intake.PreVisitIntakes
WHERE DoctorId = '<doctor-guid>';
```

### Verify Submission After Status Update
```sql
SELECT Id, Status, ReviewedAt, ReviewedByDoctorId
FROM intake.PreVisitIntakes
WHERE Id = '<submission-guid>';
```

### Verify Doctor Record
```sql
SELECT d.Id, d.ClinicName, d.UserId, u.Email
FROM auth.Doctors d
JOIN auth.AspNetUsers u ON d.UserId = u.Id
WHERE u.Email = 'doctor@example.com';
```

### Verify Role Assignment
```sql
SELECT u.Email, r.Name as Role
FROM auth.AspNetUsers u
JOIN auth.AspNetUserRoles ur ON u.Id = ur.UserId
JOIN auth.AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'doctor@example.com';
```

### Verify Permission Claims
```sql
SELECT r.Name as Role, rc.ClaimValue as Permission
FROM auth.AspNetRoleClaims rc
JOIN auth.AspNetRoles r ON rc.RoleId = r.Id
ORDER BY r.Name, rc.ClaimValue;
```

### Verify Hangfire Jobs
```sql
SELECT Id, InvocationData, StateName, CreatedAt
FROM Hangfire.Job
ORDER BY CreatedAt DESC;
```

### Verify Email Confirmation Flow
```sql
-- After registration
SELECT EmailConfirmed FROM auth.AspNetUsers WHERE Email = 'x';
-- Expected: 0 (false)

-- After confirm-email
SELECT EmailConfirmed FROM auth.AspNetUsers WHERE Email = 'x';
-- Expected: 1 (true)

-- OTP status
SELECT Code, IsUsed, Purpose FROM auth.OtpEntries
WHERE UserId = (SELECT Id FROM auth.AspNetUsers WHERE Email = 'x');
-- Expected: IsUsed=1 for old OTPs
```

---

# Summary Statistics

| Module | Endpoints | Total Test Cases |
|--------|-----------|-----------------|
| Auth - Registration | 1 | 20 |
| Auth - Confirm Email | 1 | 11 |
| Auth - Resend Confirmation | 1 | 6 |
| Auth - Login | 1 | 14 |
| Auth - Refresh Token | 1 | 10 |
| Auth - Revoke Token | 1 | 6 |
| Auth - Forget Password | 1 | 5 |
| Auth - Reset Password | 1 | 13 |
| Intake - Create Schema | 1 | 28 |
| Intake - Update Schema | 1 | 15 |
| Intake - Publish Schema | 1 | 9 |
| Intake - List Schemas | 1 | 7 |
| Intake - Get Schema | 1 | 6 |
| Intake - Get Default | 1 | 4 |
| Intake - QR Generation | 1 | 11 |
| Public - Get Form | 1 | 8 |
| Public - Submit Form | 1 | 21 |
| Intake - List Submissions | 1 | 10 |
| Intake - Get Submission | 1 | 7 |
| Intake - Update Status | 1 | 13 |
| Intake - Convert to Patient | 1 | 8 |
| **Total** | **21** | **232** |
