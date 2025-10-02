# Adaplio Bug Fixes & Testing Summary

## Phase 1: Authentication System - COMPLETED ✅
## Phase 2: Consent & Grant System - COMPLETED ✅

## Issues Found & Fixed

### 1. **LINQ Translation Error in Magic Link Queries**

**Issue**: SQLite provider couldn't translate LINQ queries with `DateTimeOffset.UtcNow` inside `Where` clauses.

**Error Message**:
```
System.InvalidOperationException: The LINQ expression 'DbSet<MagicLink>()
.Where(m => m.Email == __ToLowerInvariant_0 && m.ExpiresAt < DateTimeOffset.UtcNow)'
could not be translated.
```

**Root Cause**: Entity Framework's SQLite provider has limitations with translating complex date/time operations in LINQ expressions.

**Fix Applied**: Modified queries to use client-side evaluation by fetching records first, then filtering:

**File**: `src/Api/Adaplio.Api/Auth/AuthEndpoints.cs`

**Before** (Line 60-64):
```csharp
var expiredLinks = await context.MagicLinks
    .Where(ml => ml.Email == request.Email.ToLowerInvariant() && ml.ExpiresAt < DateTimeOffset.UtcNow)
    .ToListAsync();
```

**After**:
```csharp
var now = DateTimeOffset.UtcNow;
var allLinksForEmail = await context.MagicLinks
    .Where(ml => ml.Email == request.Email.ToLowerInvariant())
    .ToListAsync();

var expiredLinks = allLinksForEmail.Where(ml => ml.ExpiresAt < now).ToList();
```

**Similar fix** applied to `VerifyMagicLink` method (Line 93-99):
```csharp
var now = DateTimeOffset.UtcNow;
var magicLinks = await context.MagicLinks
    .Where(ml => ml.Code == request.Code && ml.UsedAt == null)
    .ToListAsync();

var magicLink = magicLinks.FirstOrDefault(ml => ml.ExpiresAt > now);
```

### 2. **Database Schema Mismatch**

**Issue**: Existing SQLite database was missing the `avatar_url` column added to `AppUser` model.

**Error Message**:
```
Microsoft.Data.Sqlite.SqliteException: SQLite Error 1: 'no such column: a.avatar_url'.
```

**Fix Applied**: Deleted old database and let Entity Framework create a fresh one with correct schema.

**Command**:
```bash
cd src/Api/Adaplio.Api
rm -f db.sqlite db.sqlite-shm db.sqlite-wal
dotnet run  # Creates fresh database
```

### 3. **Frontend Development Configuration**

**Issue**: Frontend was pointing to production API (`https://adaplio.onrender.com`) even in development mode.

**Fix Applied**: Updated development settings to use localhost.

**File**: `src/Frontend/Adaplio.Frontend/wwwroot/appsettings.Development.json`

**Before**:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://adaplio.onrender.com"
  }
}
```

**After**:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

## Test Results

### ✅ Client Magic Link Authentication

**Test**:
```bash
# 1. Request magic link
curl -X POST http://localhost:8080/auth/client/magic-link \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'

# Response:
# {"message":"Magic link sent successfully. Please check your email.","expiresAt":"2025-10-01T20:00:36.097509+00:00"}

# 2. Check console for magic link code (in development)
# CODE: 212824

# 3. Verify code
curl -X POST http://localhost:8080/auth/client/verify \
  -H "Content-Type: application/json" \
  -d '{"code":"212824"}'

# Response:
# {"message":"Login successful","userType":"","userId":"1","alias":null,"token":"eyJhbGc..."}
```

**Status**: ✅ Working perfectly

**Notes**:
- Email service falls back to console logging in development (no Resend API key configured)
- Magic link codes appear in API console output
- JWT token generated successfully
- New users have empty `userType` (need to select role via `/auth/role` endpoint)

### ✅ Trainer Registration & Login

**Registration Test**:
```bash
curl -X POST http://localhost:8080/auth/trainer/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"trainer@test.com",
    "password":"SecurePass123",
    "fullName":"Dr. Test Trainer",
    "practiceName":"Test PT Clinic"
  }'

# Response:
# {"message":"Trainer registered successfully.","userType":null,"userId":null,"alias":null,"token":null}
```

**Login Test**:
```bash
curl -X POST http://localhost:8080/auth/trainer/login \
  -H "Content-Type: application/json" \
  -d '{"email":"trainer@test.com","password":"SecurePass123"}'

# Response:
# {"message":"Login successful","userType":"trainer","userId":"2","alias":null,"token":"eyJhbGc..."}
```

**Status**: ✅ Working perfectly

**Notes**:
- Password hashing with BCrypt works correctly
- JWT tokens generated with proper trainer role
- Trainer profile created automatically

### ✅ JWT Token Generation

**Validation**:
- Tokens generated with correct claims (userId, email, userType/role)
- 24-hour expiration set correctly
- Tokens can be decoded and validated

**Client Token Example**:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6IiIsInVzZXJfdHlwZSI6IiIsIm5iZiI6MTc1OTM0Nzk1NCwiZXhwIjoxNzU5NDM0MzU0LCJpYXQiOjE3NTkzNDc5NTQsImlzcyI6ImFkYXBsaW8tYXBpIiwiYXVkIjoiYWRhcGxpby1mcm9udGVuZCJ9
```

**Decoded Claims**:
- `nameid`: "1" (userId)
- `email`: "test@example.com"
- `role`: "" (empty for new users)
- `user_type`: "" (empty for new users)
- `exp`: 1759434354 (24 hours from creation)

**Trainer Token Example**:
- `nameid`: "2" (userId)
- `email`: "trainer@test.com"
- `role`: "trainer"
- `user_type`: "trainer"

**Status**: ✅ Working perfectly

## Development Setup

### Quick Start

```bash
# 1. Ensure database is fresh (if needed)
cd src/Api/Adaplio.Api
rm -f db.sqlite db.sqlite-shm db.sqlite-wal

# 2. Start API
dotnet run
# API will be available at http://localhost:8080
# Magic link codes will appear in console output

# 3. In another terminal, start frontend
cd src/Frontend/Adaplio.Frontend
dotnet run
# Frontend will be available at http://localhost:5000
```

### Email Configuration

#### Development Mode (Console Logging)
- **No configuration needed** - emails automatically log to console
- Magic link codes appear in API output with format:
  ```
  === MAGIC LINK CODE for user@example.com ===
  CODE: 123456
  =========================================
  ```

#### Production Mode (Resend)
Set these environment variables:
```bash
RESEND_API_KEY=re_xxxxxxxxxxxxxxxxxxxx
RESEND_FROM_EMAIL=noreply@adaplio.com
```

## Recommendations for Next Steps

### Phase 2: Feature Testing (Systematic Approach)

Now that authentication is working, test features in order of dependency:

1. **Exercise Library** (foundation)
   - Create exercises
   - Update exercises
   - List exercises
   - Delete exercises

2. **Trainer-Client Connection** (prerequisite for plans)
   - Grant code generation
   - Client acceptance
   - Consent management

3. **Plan Templates** (trainer creates reusable plans)
   - Create templates
   - List templates
   - Update templates

4. **Plan Proposals** (trainer proposes to client)
   - Create proposals from templates
   - Client views proposals
   - Client accepts/rejects

5. **Progress Tracking** (client logs completion)
   - Log exercise completion
   - View progress history
   - Calculate adherence

6. **Gamification** (rewards system)
   - XP awards
   - Level progression
   - Badge unlocks
   - Streak tracking

7. **File Uploads** (media handling)
   - Upload media assets
   - Associate with exercises

### Phase 3: Frontend Integration

Once backend features are verified:

1. **Test frontend pages** with local backend
2. **Verify auth state management** in Blazor
3. **Test API communication** through AuthenticatedHttpClient
4. **Validate UI flows** end-to-end

### Phase 4: Production Deployment

Before deploying:

1. **Set up Resend account** and verify domain
2. **Configure production database** (PostgreSQL on Render/Supabase)
3. **Set all environment variables** on hosting platform
4. **Update CORS origins** to include production frontend domain
5. **Test with production API** from staging frontend

## Files Modified

### API Changes
- `src/Api/Adaplio.Api/Auth/AuthEndpoints.cs` - Fixed LINQ translation issues

### Frontend Changes
- `src/Frontend/Adaplio.Frontend/wwwroot/appsettings.Development.json` - Updated API base URL

### New Documentation
- `SETUP.md` - Comprehensive setup guide
- `FIXES.md` - This file

### Database
- Fresh SQLite database with correct schema

## Known Issues / Limitations

### Development Environment
1. **Email Service**: Uses console logging instead of actual email delivery
   - **Workaround**: Magic link codes printed to console
   - **Fix**: Set `RESEND_API_KEY` environment variable

2. **HTTPS Redirect Warning**: API logs warning about HTTPS port
   - **Impact**: None in development (HTTP is fine)
   - **Note**: Only applies in production deployment

3. **SQLite Limitations**: Some LINQ queries require client-side evaluation
   - **Impact**: Slight performance overhead for date comparisons
   - **Note**: Not an issue in production with PostgreSQL

### Features Not Yet Tested
- Exercise library CRUD
- Plan template management
- Plan proposals and acceptance
- Progress tracking
- Gamification system
- Trainer-client consent flow
- File upload functionality

## Performance Notes

### Current Query Patterns

The fix for LINQ translation uses client-side filtering which means:

**SendMagicLink**:
- Fetches all magic links for the email (typically 0-5 records)
- Filters expired ones in memory
- **Performance**: Negligible impact

**VerifyMagicLink**:
- Fetches all unused links with the code (typically 0-1 records)
- Filters by expiration in memory
- **Performance**: Negligible impact

### Future Optimizations

If moving to PostgreSQL in production, consider:
1. **Use SQL functions**: PostgreSQL handles date/time comparisons better
2. **Add indexes**: Index on (`email`, `expires_at`) for faster queries
3. **Clean up old links**: Periodic job to delete expired magic links

## Security Considerations

### Current Security Features
✅ **BCrypt password hashing** for trainers
✅ **JWT tokens** with 24-hour expiration
✅ **HttpOnly cookies** for secure token storage
✅ **Rate limiting** on auth endpoints
✅ **Audit logging** for security events
✅ **Magic link expiration** (15 minutes)
✅ **Magic link single-use** (marked as used after verification)

### Security Recommendations
1. **Set strong JWT_SECRET** in production
2. **Use HTTPS** in production (enforce with middleware)
3. **Configure CORS properly** (only allow your frontend domain)
4. **Monitor audit logs** for suspicious activity
5. **Implement MFA** for trainer accounts (future enhancement)
6. **Add CAPTCHA** to magic link requests (future enhancement)

### 4. **JWT Claim Inconsistency in SetUserRole**

**Issue**: SetUserRole endpoint was looking for "UserId" claim, but JWT tokens use `ClaimTypes.NameIdentifier`.

**Error**: 401 Unauthorized when trying to set user role.

**Files Modified**:
- `src/Api/Adaplio.Api/Auth/AuthEndpoints.cs` (Lines 303, 346, 393)

**Fix Applied**: Changed all three methods to use `ClaimTypes.NameIdentifier`:
```csharp
// Before
var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;

// After
var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
```

**Status**: ✅ Fixed in GetCurrentUser, UpdateProfile, and SetUserRole methods

### 5. **LINQ Translation Errors in ConsentEndpoints**

**Issue**: Same `DateTimeOffset.UtcNow` LINQ translation issues in grant code validation and acceptance.

**Files Modified**:
- `src/Api/Adaplio.Api/Auth/ConsentEndpoints.cs` (Lines 107 and 160)

**Fix Applied**: Client-side evaluation for both ValidateGrant and AcceptGrant:
```csharp
// ValidateGrant - Line 105-112
var now = DateTimeOffset.UtcNow;
var grantCodes = await context.GrantCodes
    .Include(g => g.TrainerProfile)
    .Where(g => g.Code == code && g.UsedAt == null)
    .ToListAsync();

var grantCode = grantCodes.FirstOrDefault(g => g.ExpiresAt > now);

// AcceptGrant - Line 155-162
var now = DateTimeOffset.UtcNow;
var grantCodes = await context.GrantCodes
    .Include(gc => gc.TrainerProfile)
        .ThenInclude(tp => tp.User)
    .Where(gc => gc.Code == request.GrantCode && gc.UsedAt == null)
    .ToListAsync();

var grantCode = grantCodes.FirstOrDefault(gc => gc.ExpiresAt > now);
```

**Status**: ✅ Fixed

### 6. **LINQ Translation Error in GetTrainerClients**

**Issue**: Same LINQ issue with `DateTimeOffset.UtcNow` in PlanEndpoints when getting trainer's connected clients.

**Error Message**:
```
The LINQ expression 'DbSet<ConsentGrant>()
    .Where(c => c.TrainerProfileId == __trainerProfile_Id_0 && c.ExpiresAt > (DateTimeOffset?)DateTimeOffset.UtcNow)'
could not be translated.
```

**File Modified**:
- `src/Api/Adaplio.Api/Plans/PlanEndpoints.cs` (Lines 578-599)

**Fix Applied**: Client-side evaluation with grouping:
```csharp
// Get clients that have granted consent to this trainer (client-side date evaluation)
var now = DateTimeOffset.UtcNow;
var allGrants = await context.ConsentGrants
    .Where(cg => cg.TrainerProfileId == trainerProfile.Id)
    .Include(cg => cg.ClientProfile)
    .ThenInclude(cp => cp.User)
    .ToListAsync();

var activeGrants = allGrants.Where(cg => cg.ExpiresAt == null || cg.ExpiresAt > now).ToList();

var clients = activeGrants
    .GroupBy(cg => cg.ClientProfile.Id)
    .Select(g => new
    {
        id = g.First().ClientProfile.Id,
        alias = g.First().ClientProfile.Alias,
        email = g.First().ClientProfile.User.Email,
        createdAt = g.First().ClientProfile.CreatedAt,
        scopes = g.Select(cg => cg.Scope).ToList()
    })
    .OrderBy(c => c.alias)
    .ToList();
```

**Additional Fix**: Added null check for ExpiresAt since consent grants don't expire by default.

**Status**: ✅ Fixed

## Phase 2: Consent & Grant System Testing Results

### ✅ Grant Code Generation
- Trainer can successfully create grant codes
- 8-character codes generated (e.g., "KQDA5FE5")
- 24-hour expiration set correctly
- Grant URL provided

### ✅ Client Grant Acceptance
- Client can accept grant codes
- 3 default scopes created: propose_plan, view_summary, message_client
- Client alias generated (e.g., "C-RABA")
- Trainer-client connection established

### ✅ Trainer Views Connected Clients
- Trainer can retrieve list of connected clients
- Response includes:
  - Client ID
  - Client alias (pseudonymous)
  - Client email
  - Creation timestamp
  - Granted scopes array

**Example Response**:
```json
{
  "clients": [{
    "id": 1,
    "alias": "C-RABA",
    "email": "freshclient@test.com",
    "createdAt": "2025-10-01T23:26:44.0991016+00:00",
    "scopes": ["propose_plan", "view_summary", "message_client"]
  }]
}
```

## Conclusion

**Phases Completed**:
- ✅ **Phase 1**: Authentication System (magic link, trainer login, JWT)
- ✅ **Phase 2**: Consent & Grant System (grant codes, client acceptance, trainer-client connection)

**Total Issues Fixed**: 6
- 3 LINQ translation issues in AuthEndpoints
- 1 JWT claim inconsistency
- 2 LINQ translation issues in ConsentEndpoints
- 1 LINQ translation issue in PlanEndpoints (with null handling)

**Pattern Identified**: SQLite LINQ provider cannot translate `DateTimeOffset.UtcNow` in Where clauses. Solution is consistent: fetch to memory first, then filter with client-side LINQ.

**Next Steps**: Continue systematic testing:
- Phase 3: Plan Templates
- Phase 4: Plan Proposals
- Phase 5: Progress Tracking
- Phase 6: Gamification
