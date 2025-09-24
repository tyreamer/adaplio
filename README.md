# Adaplio

A privacy-first, gamified physical therapy adherence app. Trainers propose plans, clients accept and track them, and progress is visualized week-by-week.

---

## Stack

- **Frontend**: Blazor WebAssembly PWA + MudBlazor
- **Backend**: ASP.NET Core Minimal APIs
- **Database**: SQLite (local) → Supabase Postgres (cloud)
- **Storage**: Supabase Buckets (later Azure Blob)
- **CI/CD**: GitHub Actions
- **Dev tools**: Docker Compose (MailHog), EF Core

---

## Quick Start (Dev)

### Prerequisites
- .NET 8 SDK
- Docker (for MailHog)

### Setup

```bash
# Clone repo
git clone https://github.com/YOURNAME/adaplio.git
cd adaplio

# Launch local services (MailHog for email testing)
docker compose up -d

# Restore dependencies
dotnet restore

# Run API (SQLite database will be created automatically)
cd src/Api/Adaplio.Api
dotnet run

# In another terminal, run Frontend
cd src/Frontend/Adaplio.Frontend
dotnet run
```

### URLs
- **Frontend**: http://localhost:5000
- **API**: http://localhost:5001
- **MailHog UI**: http://localhost:8025 (email testing)

---

## Database Management

### Entity Framework Core Migrations

The project uses EF Core with SQLite for local development. The database will be created automatically when you first run the API.

#### Migration Commands

```bash
# Create a new migration (run from src/Api/Adaplio.Api)
dotnet ef migrations add <MigrationName>

# Update database to latest migration
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# View migration history
dotnet ef migrations list

# Generate SQL script for migration
dotnet ef migrations script
```

#### Database Configuration

- **Local**: SQLite database (`db.sqlite`) - created automatically
- **Environment Variable**: Set `DB_CONNECTION` to override connection string
- **Production**: Will use PostgreSQL (Supabase)

#### Domain Model

The database includes the following main entities:
- `app_user` - Base user authentication
- `client_profile` / `trainer_profile` - Role-specific profiles
- `consent_grant` - Privacy permissions between clients/trainers
- `plan_template` / `plan_proposal` / `plan_instance` - Therapy plan workflow
- `exercise` / `exercise_instance` - Exercise definitions and instances
- `progress_event` - Client activity logging
- `adherence_week` - Weekly adherence summaries
- `gamification` - XP, streaks, and achievement tracking
- `media_asset` / `transcript` / `extraction_result` - Media processing pipeline
- `magic_link` - Passwordless authentication tokens

---

## Authentication

### Overview

Adaplio implements a dual authentication system:
- **Clients**: Passwordless magic link authentication (email-based)
- **Trainers**: Traditional email/password authentication

### Testing Authentication

#### Prerequisites
1. Start MailHog: `docker compose up -d`
2. Run the API: `cd src/Api/Adaplio.Api && dotnet run`
3. Run the Frontend: `cd src/Frontend/Adaplio.Frontend && dotnet run`

#### Client Authentication Flow

1. **Request Magic Link**
   ```bash
   curl -X POST https://localhost:5001/auth/client/magic-link \
   -H "Content-Type: application/json" \
   -d '{"email": "client@example.com"}'
   ```

2. **Check MailHog** - Visit http://localhost:8025 to see the email with the 6-digit code

3. **Verify Code**
   ```bash
   curl -X POST https://localhost:5001/auth/client/verify \
   -H "Content-Type: application/json" \
   -d '{"code": "123456"}' \
   -c cookies.txt
   ```

#### Trainer Authentication Flow

1. **Register Trainer**
   ```bash
   curl -X POST https://localhost:5001/auth/trainer/register \
   -H "Content-Type: application/json" \
   -d '{
     "email": "trainer@example.com",
     "password": "securepass123",
     "fullName": "Dr. Jane Smith",
     "practiceName": "Smith Physical Therapy"
   }'
   ```

2. **Login Trainer**
   ```bash
   curl -X POST https://localhost:5001/auth/trainer/login \
   -H "Content-Type: application/json" \
   -d '{
     "email": "trainer@example.com",
     "password": "securepass123"
   }' \
   -c cookies.txt
   ```

#### Frontend Testing

- **Client Login**: Visit http://localhost:5000/auth/client/login
- **Trainer Login**: Visit http://localhost:5000/auth/trainer/login
- **Trainer Register**: Visit http://localhost:5000/auth/trainer/register

#### Security Features

- **Rate Limiting**: 10 requests per minute on `/auth/*` endpoints
- **HttpOnly Cookies**: JWT tokens stored securely
- **CORS**: Configured for frontend origin only
- **Password Hashing**: BCrypt for trainer passwords
- **Magic Link Expiry**: 15-minute expiration for client codes
- **Client Pseudonymity**: Auto-generated aliases (e.g., "C-7Q2F")

---

## Consent Grants & Pairing

### Overview

The consent grants system allows trainers to invite clients and establish secure connections with explicit permissions. Clients are identified by stable, privacy-preserving aliases.

### Testing Consent & Pairing Flow

#### Prerequisites
1. Start the services as described in the Authentication section
2. Have both trainer and client accounts ready (or use the seed endpoint)

#### Quick Demo Setup

Use the development seed endpoint to create demo accounts:

```bash
curl -X POST https://localhost:5001/api/dev/grants/seed
```

This creates:
- Demo trainer: `demo-trainer@adaplio.local` / `DemoPass123`
- Demo client: `demo-client@adaplio.local` (passwordless)
- Pre-connected relationship with grant code

#### Manual Flow Testing

1. **Trainer Creates Grant Code**
   ```bash
   # Login as trainer first
   curl -X POST https://localhost:5001/auth/trainer/login \
   -H "Content-Type: application/json" \
   -d '{"email": "trainer@example.com", "password": "securepass123"}' \
   -c cookies.txt

   # Create grant code
   curl -X POST https://localhost:5001/api/trainer/grants \
   -H "Content-Type: application/json" \
   -d '{}' \
   -b cookies.txt
   ```

   Response includes:
   - `grantCode`: 8-character code (e.g., "ABC12345")
   - `url`: Full invitation URL
   - `expiresAt`: 24-hour expiration timestamp

2. **Client Accepts Grant**
   ```bash
   # Login as client first
   curl -X POST https://localhost:5001/auth/client/magic-link \
   -H "Content-Type: application/json" \
   -d '{"email": "client@example.com"}'

   # Check MailHog for code, then verify
   curl -X POST https://localhost:5001/auth/client/verify \
   -H "Content-Type: application/json" \
   -d '{"email": "client@example.com", "code": "123456"}' \
   -c client-cookies.txt

   # Accept the grant using the code from step 1
   curl -X POST https://localhost:5001/api/client/grants/accept \
   -H "Content-Type: application/json" \
   -d '{"grantCode": "ABC12345"}' \
   -b client-cookies.txt
   ```

3. **Trainer Views Connected Clients**
   ```bash
   curl -X GET https://localhost:5001/api/trainer/clients \
   -b cookies.txt
   ```

#### Frontend Testing

- **Trainer Dashboard**: Login at `/auth/trainer/login` then visit `/trainer/dashboard`
- **Client Grant Acceptance**: Visit `/grant/{grantCode}` (auto-redirects through login if needed)

#### Key Features

- **Grant Code Expiry**: 24-hour expiration with usage tracking
- **Stable Client Aliases**: SHA256-based pseudonyms (format: C-XXXX)
- **Default Scopes**: `propose_plan`, `view_summary`, `message_client`
- **Duplicate Protection**: Prevents multiple connections between same client-trainer pair
- **Privacy-First**: Clients identified only by aliases to trainers

---

## Plan Flow (Templates → Proposals → Acceptance → Board)

### Overview

The plan management system enables trainers to create reusable templates, propose them to clients, handle client acceptance/modifications, and provide clients with a weekly board view for execution. The system supports Monday-start weeks and granular exercise scheduling.

### Testing Plan Flow

#### Prerequisites
1. Complete the Consent & Pairing flow to have connected client-trainer relationships
2. Have active authentication sessions (cookies) for both client and trainer

#### Demo Setup (Recommended)

Use the development seed endpoint to create a complete demo setup:

```bash
curl -X POST https://localhost:5001/api/dev/templates/seed
```

This creates:
- Demo trainer: `demo-trainer@adaplio.local` / `DemoPass123`
- Demo client: `demo-client@adaplio.local` (passwordless)
- Sample template with 3 exercises
- Pre-created proposal ready for acceptance
- Proper consent grants for plan proposal and viewing

#### Manual Flow Testing

**1. Trainer Creates Template**
```bash
# Login as trainer first
curl -X POST https://localhost:5001/auth/trainer/login \
-H "Content-Type: application/json" \
-d '{"email": "trainer@example.com", "password": "securepass123"}' \
-c cookies.txt

# Create a plan template
curl -X POST https://localhost:5001/api/trainer/templates \
-H "Content-Type: application/json" \
-d '{
  "name": "Upper Body Recovery",
  "description": "Gentle upper body rehabilitation plan",
  "category": "Rehabilitation",
  "durationWeeks": 4,
  "isPublic": false,
  "items": [
    {
      "exerciseName": "Wall Push-ups",
      "targetSets": 3,
      "targetReps": 10,
      "days": ["Monday", "Wednesday", "Friday"],
      "notes": "Start gently, focus on form"
    },
    {
      "exerciseName": "Shoulder Rolls",
      "targetSets": 2,
      "targetReps": 15,
      "days": ["Monday", "Wednesday", "Friday"],
      "notes": "Keep movements slow and controlled"
    }
  ]
}' \
-b cookies.txt
```

**2. View/Edit Templates**
```bash
# Get all trainer templates
curl -X GET https://localhost:5001/api/trainer/templates \
-b cookies.txt

# Update a template
curl -X PUT https://localhost:5001/api/trainer/templates/1 \
-H "Content-Type: application/json" \
-d '{
  "name": "Updated Upper Body Plan",
  "description": "Modified rehabilitation plan",
  "items": [...]
}' \
-b cookies.txt

# Delete a template (soft delete)
curl -X DELETE https://localhost:5001/api/trainer/templates/1 \
-b cookies.txt
```

**3. Trainer Creates Proposal**
```bash
# Propose template to client
curl -X POST https://localhost:5001/api/trainer/proposals \
-H "Content-Type: application/json" \
-d '{
  "clientAlias": "C-7Q2F",
  "templateId": 1,
  "startsOn": "2024-12-30",
  "message": "Hi! I created this plan for your recovery. Please review and accept."
}' \
-b cookies.txt

# View trainer proposals
curl -X GET https://localhost:5001/api/trainer/proposals \
-b cookies.txt
```

**4. Client Views and Accepts Proposal**
```bash
# Login as client first
curl -X POST https://localhost:5001/auth/client/magic-link \
-H "Content-Type: application/json" \
-d '{"email": "client@example.com"}'

# Check MailHog for code, then verify
curl -X POST https://localhost:5001/auth/client/verify \
-H "Content-Type: application/json" \
-d '{"email": "client@example.com", "code": "123456"}' \
-c client-cookies.txt

# View client proposals
curl -X GET https://localhost:5001/api/client/proposals \
-b client-cookies.txt

# View specific proposal details
curl -X GET https://localhost:5001/api/client/proposals/1 \
-b client-cookies.txt

# Accept proposal (all exercises)
curl -X POST https://localhost:5001/api/client/proposals/1/accept \
-H "Content-Type: application/json" \
-d '{"acceptAll": true}' \
-b client-cookies.txt

# Accept proposal (select specific exercises)
curl -X POST https://localhost:5001/api/client/proposals/1/accept \
-H "Content-Type: application/json" \
-d '{"acceptItemIds": [0, 1]}' \
-b client-cookies.txt
```

**5. Client Views Plans and Weekly Board**
```bash
# View active plans
curl -X GET https://localhost:5001/api/client/plans \
-b client-cookies.txt

# Get weekly board (current week)
curl -X GET https://localhost:5001/api/client/board \
-b client-cookies.txt

# Get specific week board
curl -X GET https://localhost:5001/api/client/board?weekStart=2024-12-30 \
-b client-cookies.txt

# Quick log exercise completion
curl -X POST https://localhost:5001/api/client/board/quick-log \
-H "Content-Type: application/json" \
-d '{
  "exerciseInstanceId": 1,
  "completed": true,
  "reps": 10
}' \
-b client-cookies.txt
```

#### Frontend Testing

- **Trainer Template Management**: Login as trainer, visit `/trainer/templates`
- **Trainer Dashboard**: Login as trainer, visit `/trainer/dashboard`
- **Client Proposals**: Login as client, visit `/proposals` (when implemented)
- **Client Weekly Board**: Login as client, visit `/board`

#### Response Examples

**Template Response:**
```json
{
  "id": 1,
  "name": "Upper Body Recovery",
  "description": "Gentle upper body rehabilitation plan",
  "category": "Rehabilitation",
  "durationWeeks": 4,
  "isPublic": false,
  "items": [
    {
      "id": 1,
      "exerciseName": "Wall Push-ups",
      "targetSets": 3,
      "targetReps": 10,
      "days": ["Monday", "Wednesday", "Friday"],
      "notes": "Start gently, focus on form"
    }
  ]
}
```

**Proposal Response:**
```json
{
  "id": 1,
  "trainerName": "Dr. Jane Smith",
  "clientAlias": "C-7Q2F",
  "proposalName": "Upper Body Recovery",
  "message": "Hi! I created this plan for your recovery.",
  "status": "pending",
  "startsOn": "2024-12-30",
  "items": [...]
}
```

**Weekly Board Response:**
```json
{
  "weekStart": "2024-12-30",
  "weekEnd": "2025-01-05",
  "days": [
    {
      "dayName": "Monday",
      "date": "2024-12-30",
      "dayOfWeek": 1,
      "exercises": [
        {
          "exerciseInstanceId": 1,
          "exerciseName": "Wall Push-ups",
          "targetSets": 3,
          "targetReps": 10,
          "status": "planned"
        }
      ]
    }
  ]
}
```

#### Key Features

- **Template Reusability**: Create once, propose multiple times
- **Immutable Proposals**: Snapshot system preserves original plan details
- **Selective Acceptance**: Clients can accept subset of exercises
- **Monday-Start Weeks**: Consistent weekly view starting Mondays
- **Day-of-Week Scheduling**: Exercises assigned to specific weekdays
- **Quick Logging**: Simple "Mark Done" buttons on weekly board
- **Consent-Gated**: Requires `propose_plan` scope for trainer-client proposals
- **Soft Delete**: Templates marked deleted but preserved for historical proposals

---

## Progress & Adherence (P5)

### Overview

The progress tracking system allows clients to log exercise completion and provides adherence analytics for both clients and trainers. Progress events support granular tracking (sets, reps, hold time, difficulty, pain levels) while adherence is calculated weekly.

### Testing Progress & Adherence Flow

#### Prerequisites
1. Complete the Consent & Pairing flow to have connected client-trainer relationships
2. Have active authentication sessions (cookies) for both client and trainer

#### Client Progress Logging

1. **Log Exercise Progress** (as client)
   ```bash
   # Login as client first
   curl -X POST https://localhost:5001/auth/client/magic-link \
   -H "Content-Type: application/json" \
   -d '{"email": "client@example.com"}'

   # Verify with code from MailHog
   curl -X POST https://localhost:5001/auth/client/verify \
   -H "Content-Type: application/json" \
   -d '{"email": "client@example.com", "code": "123456"}' \
   -c client-cookies.txt

   # Log completed exercise
   curl -X POST https://localhost:5001/api/client/progress \
   -H "Content-Type: application/json" \
   -d '{
     "exerciseInstanceId": 1,
     "eventType": "exercise_completed",
     "setsCompleted": 3,
     "repsCompleted": 10,
     "holdSecondsCompleted": 30,
     "difficultyRating": 7,
     "painLevel": 2,
     "notes": "Felt good today, completed all sets"
   }' \
   -b client-cookies.txt
   ```

2. **Log Partial/Set Completion** (as client)
   ```bash
   # Log individual set completion
   curl -X POST https://localhost:5001/api/client/progress \
   -H "Content-Type: application/json" \
   -d '{
     "exerciseInstanceId": 1,
     "eventType": "set_completed",
     "setsCompleted": 1,
     "repsCompleted": 8,
     "holdSecondsCompleted": 25,
     "difficultyRating": 6,
     "painLevel": 3,
     "notes": "Struggled with last few reps"
   }' \
   -b client-cookies.txt
   ```

3. **View Client Adherence Summary** (as client)
   ```bash
   curl -X GET https://localhost:5001/api/client/progress/summary \
   -b client-cookies.txt
   ```

#### Trainer Adherence Viewing

1. **View Client Adherence Details** (as trainer)
   ```bash
   # Login as trainer first
   curl -X POST https://localhost:5001/auth/trainer/login \
   -H "Content-Type: application/json" \
   -d '{"email": "trainer@example.com", "password": "securepass123"}' \
   -c cookies.txt

   # View specific client's adherence (requires view_summary consent)
   curl -X GET https://localhost:5001/api/trainer/clients/C-7Q2F/adherence \
   -b cookies.txt
   ```

#### Frontend Testing

- **Client Progress Logging**: Login as client, visit `/progress`
- **Client Adherence Chart**: Login as client, visit `/adherence`
- **Trainer Dashboard**: Login as trainer, visit `/trainer/dashboard` - click "View Details" on any client

#### Response Examples

**Client Adherence Summary Response:**
```json
{
  "clientAlias": "C-7Q2F",
  "weeklyData": [
    {
      "year": 2024,
      "weekNumber": 52,
      "weekStartDate": "2024-12-23",
      "adherencePercentage": 85.0,
      "completedCount": 17,
      "plannedCount": 20
    }
  ],
  "overallAdherence": 78.5
}
```

**Trainer Client Adherence Response:**
```json
{
  "clientAlias": "C-7Q2F",
  "recentWeeks": [
    {
      "year": 2024,
      "weekNumber": 52,
      "weekStartDate": "2024-12-23",
      "adherencePercentage": 85.0,
      "completedCount": 17,
      "plannedCount": 20
    }
  ],
  "currentWeekAdherence": 85.0,
  "overallAdherence": 78.5
}
```

#### Key Features

- **Granular Progress Events**: Track sets, reps, hold time, difficulty (1-10), pain levels (1-10)
- **Event Types**: `exercise_completed`, `set_completed`, `session_completed`
- **Weekly Adherence Calculation**: Automatic weekly summaries with percentage calculations
- **Privacy-Compliant**: Trainers only see aggregated adherence data, not detailed logs
- **Consent-Gated**: Trainers need `view_summary` scope to access client adherence
- **Real-time Updates**: Progress logging immediately updates weekly adherence calculations

---

## Project Structure

```
/src
├── /Api              # ASP.NET Core 8 minimal API
├── /Frontend         # Blazor WASM PWA with MudBlazor
├── /Jobs             # Azure Functions/Containers stubs for media pipeline
└── /Shared           # Shared DTOs/domain primitives

/infra
├── docker-compose.yml        # MailHog for local email testing
├── .devcontainer/           # VS Code dev container setup
└── .github/workflows/       # CI/CD pipeline
```

---

## Features

### Current
- ✅ Blazor WebAssembly PWA with offline support
- ✅ MudBlazor UI with Poppins font and dark/light toggle
- ✅ ASP.NET Core minimal API ready for development
- ✅ Docker Compose with MailHog for email testing
- ✅ GitHub Actions CI/CD pipeline
- ✅ EF Core domain models with SQLite database
- ✅ Auth system (magic link for clients, email/password for trainers)
- ✅ Consent grants & pairing system with privacy-preserving client aliases
- ✅ Progress & adherence tracking system with weekly analytics
- ✅ Plan authoring, proposal, acceptance & weekly board system

### Roadmap
- 🔲 Gamification system (XP, streaks, badges)
- 🔲 Media pipeline (upload, transcode, extract exercises)
- 🔲 Push notifications and email recaps

---

## Development

### Building
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Publishing
```bash
dotnet publish --configuration Release
```

---

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the LICENSE file for details.