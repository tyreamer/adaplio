# Adaplio Testing Guide

## Current Status

### ✅ Completed & Working
- Client magic link authentication
- Trainer registration and login
- JWT token generation and validation
- Database schema (fresh SQLite with all columns)
- Frontend development configuration (points to localhost)
- Email service (console fallback for development)

### ⚠️ Needs Testing
The following features have not been tested yet and may contain similar LINQ translation issues:

1. **Plan Templates** - Trainer CRUD operations
2. **Consent/Grant System** - Trainer-client connections
3. **Plan Proposals** - Template to proposal flow
4. **Progress Tracking** - Exercise completion logging
5. **Gamification** - XP, badges, streaks
6. **File Uploads** - Media asset handling

## Test Plan (Sequential Order)

### Prerequisites

Start the API server:
```bash
cd src/Api/Adaplio.Api
dotnet run
# API runs on http://localhost:8080
```

Create test users:
```bash
# 1. Create trainer
curl -X POST http://localhost:8080/auth/trainer/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"trainer@test.com",
    "password":"SecurePass123",
    "fullName":"Dr. Test Trainer",
    "practiceName":"Test PT Clinic"
  }'

# 2. Login and save token
TRAINER_TOKEN=$(curl -s -X POST http://localhost:8080/auth/trainer/login \
  -H "Content-Type: application/json" \
  -d '{"email":"trainer@test.com","password":"SecurePass123"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)

echo $TRAINER_TOKEN

# 3. Create client via magic link
curl -X POST http://localhost:8080/auth/client/magic-link \
  -H "Content-Type: application/json" \
  -d '{"email":"client@test.com"}'

# Check console for CODE, then verify
CLIENT_TOKEN=$(curl -s -X POST http://localhost:8080/auth/client/verify \
  -H "Content-Type: application/json" \
  -d '{"code":"INSERT_CODE_HERE"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)

echo $CLIENT_TOKEN

# 4. Set client role
curl -X POST http://localhost:8080/auth/role \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -d '{"role":"client"}'
```

---

## Phase 1: Consent & Grant System

### Test Grant Code Generation

```bash
# Trainer creates grant code
curl -X POST http://localhost:8080/api/trainer/grants \
  -H "Authorization: Bearer $TRAINER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Expected**: Grant code returned (8-character code, expiration timestamp)

**Potential Issues**:
- Check `InviteEndpoints.cs` for `DateTimeOffset.UtcNow` in LINQ queries
- Grant expiration logic may need client-side evaluation

### Test Client Accepts Grant

```bash
# Client accepts the grant (use code from previous step)
curl -X POST http://localhost:8080/api/client/grants/accept \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"grantCode":"ABCD1234"}'
```

**Expected**: Consent grant created, scopes assigned

**Potential Issues**:
- Grant lookup with expiration check
- Duplicate grant detection

### Verify Connection

```bash
# Trainer views connected clients
curl -X GET http://localhost:8080/api/trainer/clients \
  -H "Authorization: Bearer $TRAINER_TOKEN"
```

**Expected**: Client appears in trainer's client list with alias

**Known Issue**: Line 581 in `PlanEndpoints.cs`:
```csharp
.Where(cg => cg.ExpiresAt > DateTimeOffset.UtcNow)
```
This will likely fail with SQLite. **Fix needed**: Client-side evaluation like we did for auth.

---

## Phase 2: Plan Templates

### Create Template

```bash
curl -X POST http://localhost:8080/api/trainer/templates \
  -H "Authorization: Bearer $TRAINER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Upper Body Recovery",
    "description": "Gentle upper body rehab",
    "category": "Rehabilitation",
    "durationWeeks": 4,
    "isPublic": false,
    "items": [
      {
        "exerciseName": "Wall Push-ups",
        "targetSets": 3,
        "targetReps": 10,
        "days": ["Monday", "Wednesday", "Friday"],
        "notes": "Focus on form"
      },
      {
        "exerciseName": "Shoulder Rolls",
        "targetSets": 2,
        "targetReps": 15,
        "days": ["Monday", "Wednesday", "Friday"],
        "notes": "Slow and controlled"
      }
    ]
  }'
```

**Expected**: Template created with ID returned

**Potential Issues**:
- Check `PlanService.cs` for date/time LINQ queries
- Template item creation may have issues

### List Templates

```bash
curl -X GET http://localhost:8080/api/trainer/templates \
  -H "Authorization: Bearer $TRAINER_TOKEN"
```

**Expected**: Array of templates

### Update Template

```bash
curl -X PUT http://localhost:8080/api/trainer/templates/1 \
  -H "Authorization: Bearer $TRAINER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Upper Body Plan",
    "description": "Modified plan",
    "category": "Rehabilitation",
    "durationWeeks": 4,
    "isPublic": false,
    "items": [
      {
        "exerciseName": "Wall Push-ups",
        "targetSets": 4,
        "targetReps": 12,
        "days": ["Monday", "Wednesday", "Friday"],
        "notes": "Increased intensity"
      }
    ]
  }'
```

**Expected**: Updated template returned

### Delete Template

```bash
curl -X DELETE http://localhost:8080/api/trainer/templates/1 \
  -H "Authorization: Bearer $TRAINER_TOKEN"
```

**Expected**: Success message (soft delete)

---

## Phase 3: Plan Proposals

### Create Proposal

```bash
# First, get client alias from trainer/clients endpoint
CLIENT_ALIAS="C-XXXX"  # Replace with actual alias

curl -X POST http://localhost:8080/api/trainer/proposals \
  -H "Authorization: Bearer $TRAINER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clientAlias": "'$CLIENT_ALIAS'",
    "templateId": 1,
    "startsOn": "2025-10-06",
    "message": "Hi! This plan will help your recovery."
  }'
```

**Expected**: Proposal created

**Potential Issues**:
- Consent check (`propose_plan` scope required)
- Template lookup and snapshot creation
- Date parsing

### Client Views Proposals

```bash
curl -X GET http://localhost:8080/api/client/proposals \
  -H "Authorization: Bearer $CLIENT_TOKEN"
```

**Expected**: List of pending proposals

### Client Views Proposal Detail

```bash
curl -X GET http://localhost:8080/api/client/proposals/1 \
  -H "Authorization: Bearer $CLIENT_TOKEN"
```

**Expected**: Full proposal details with exercises

### Client Accepts Proposal

```bash
# Accept all exercises
curl -X POST http://localhost:8080/api/client/proposals/1/accept \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"acceptAll": true}'

# OR accept specific exercises
curl -X POST http://localhost:8080/api/client/proposals/1/accept \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"acceptItemIds": [0, 1]}'
```

**Expected**: Plan instance created, exercise instances generated

**Potential Issues**:
- Exercise instance generation for each day
- Plan instance status updates

---

## Phase 4: Client Board & Progress

### View Client Board

```bash
# Current week
curl -X GET http://localhost:8080/api/client/board \
  -H "Authorization: Bearer $CLIENT_TOKEN"

# Specific week
curl -X GET "http://localhost:8080/api/client/board?weekStart=2025-10-06" \
  -H "Authorization: Bearer $CLIENT_TOKEN"
```

**Expected**: Weekly view with exercises per day

**Potential Issues**:
- Monday calculation logic
- Exercise instance filtering by date

### Quick Log Progress

```bash
# Get exercise instance ID from board, then log completion
curl -X POST http://localhost:8080/api/client/board/quick-log \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "exerciseInstanceId": 1,
    "completed": true,
    "reps": 10
  }'
```

**Expected**: Progress event created, exercise status updated

---

## Phase 5: Progress Tracking

### Log Detailed Progress

Check `ProgressEndpoints.cs` for detailed progress logging endpoints.

```bash
curl -X POST http://localhost:8080/api/client/progress \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "exerciseInstanceId": 1,
    "eventType": "exercise_completed",
    "setsCompleted": 3,
    "repsCompleted": 10,
    "difficultyRating": 7,
    "painLevel": 2,
    "notes": "Felt good today"
  }'
```

**Expected**: Progress event created, gamification triggers

**Potential Issues**:
- Check `ProgressService.cs` for date/time queries
- Adherence calculation may use LINQ with dates

### View Progress Summary

```bash
curl -X GET http://localhost:8080/api/client/progress/summary \
  -H "Authorization: Bearer $CLIENT_TOKEN"
```

**Expected**: Weekly adherence data, overall percentage

---

## Phase 6: Gamification

### View Gamification Profile

```bash
curl -X GET http://localhost:8080/api/client/gamification \
  -H "Authorization: Bearer $CLIENT_TOKEN"
```

**Expected**: XP, level, streaks, badges

**Potential Issues**:
- Check `GamificationService.cs` for streak calculations with dates
- Badge checking logic

### Trainer Views Client Gamification

```bash
curl -X GET "http://localhost:8080/api/trainer/clients/$CLIENT_ALIAS/gamification" \
  -H "Authorization: Bearer $TRAINER_TOKEN"
```

**Expected**: Client's gamification summary

**Potential Issues**:
- Consent check (`view_summary` scope)

---

## Known LINQ Issues to Fix

### PlanEndpoints.cs:581
```csharp
// BEFORE (will fail with SQLite)
.Where(cg => cg.ExpiresAt > DateTimeOffset.UtcNow)

// AFTER (client-side evaluation)
var now = DateTimeOffset.UtcNow;
var allGrants = await context.ConsentGrants
    .Where(cg => cg.TrainerProfileId == trainerProfile.Id)
    .Include(cg => cg.ClientProfile)
    .ThenInclude(cp => cp.User)
    .ToListAsync();

var activeGrants = allGrants.Where(cg => cg.ExpiresAt > now).ToList();
```

### Search for Other Occurrences

```bash
# Find all DateTimeOffset.UtcNow in LINQ queries
grep -rn "DateTimeOffset.UtcNow" src/Api/Adaplio.Api --include="*.cs" \
  | grep -E "(Where|First|Any|Count|Select)"
```

---

## Testing Checklist

- [ ] Consent/Grant: Create grant code
- [ ] Consent/Grant: Client accepts grant
- [ ] Consent/Grant: Verify connection
- [ ] Templates: Create template
- [ ] Templates: List templates
- [ ] Templates: Update template
- [ ] Templates: Delete template
- [ ] Proposals: Create proposal
- [ ] Proposals: Client views proposals
- [ ] Proposals: Client accepts proposal
- [ ] Board: View weekly board
- [ ] Board: Quick log progress
- [ ] Progress: Detailed progress logging
- [ ] Progress: View adherence summary
- [ ] Gamification: View profile
- [ ] Gamification: Check XP/badges/streaks
- [ ] Gamification: Trainer views client stats

---

## Debugging Tips

### Check API Logs

```bash
# View API console for errors and magic link codes
# Look for:
# - "fail:" lines (errors)
# - "CODE:" lines (magic link codes)
# - SQL exceptions
# - LINQ translation errors
```

### Common Error Patterns

1. **500 Error with "LINQ expression could not be translated"**
   - Solution: Use client-side evaluation (fetch first, filter in memory)

2. **404 "Profile not found"**
   - Check user has correct role set
   - Verify token is valid

3. **403 Forbidden**
   - Check consent grants exist
   - Verify scopes are correct

4. **400 "Invalid request"**
   - Check request body format
   - Verify required fields

### Database Inspection

```bash
# View database contents
cd src/Api/Adaplio.Api
sqlite3 db.sqlite

# Useful queries
SELECT * FROM app_user;
SELECT * FROM client_profile;
SELECT * FROM trainer_profile;
SELECT * FROM consent_grant;
SELECT * FROM plan_template;
SELECT * FROM plan_proposal;
```

---

## Next Steps After Testing

1. **Document all findings** in FIXES.md
2. **Fix LINQ issues** as they're discovered
3. **Add integration tests** for critical flows
4. **Test frontend** with fixed backend
5. **Prepare for production deployment**

---

## Production Readiness Checklist

Before deploying to production:

- [ ] All features tested and working
- [ ] LINQ translation issues fixed
- [ ] Resend API key configured
- [ ] PostgreSQL database set up
- [ ] CORS origins configured
- [ ] JWT secret is strong and unique
- [ ] Environment variables set
- [ ] Frontend points to production API
- [ ] SSL/TLS configured
- [ ] Database migrations tested
- [ ] Load testing completed
- [ ] Error monitoring set up
- [ ] Backup strategy in place
