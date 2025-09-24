# Adaplio — MVP Spec

## Mission
A Duolingo-style home PT adherence app that keeps clients engaged with their physical therapy plans. Trainers propose plans; clients accept; weekly boards track progress; gamification keeps motivation high.

## Roles
- **Client**: Pseudonymous user logging progress via simple UI.
- **Trainer**: Proposes plans, views summary adherence, and messages client.

## Core Objects
- **PlanTemplate**: Reusable trainer library.
- **PlanProposal**: Immutable trainer → client offer.
- **PlanInstance**: Accepted proposal, client-owned.
- **ExerciseInstance**: Concrete items in weekly board.
- **ConsentGrant**: Scopes per trainer ↔ client (propose_plan, view_summary, message_client).
- **ProgressEvent**: Client logs (sets, reps, holds).
- **AdherenceWeek**: Summarized adherence per week.

## Privacy & Auth
- **Client login**: Passwordless magic link (later passkeys).
- **Trainer login**: Email + password + MFA.
- **Pseudonymity**: Trainers see client aliases (e.g. C-7Q2F).
- **Messaging**: Proxy channel unless client opts in.

## Non-Functional Requirements
- p95 API < 300ms.
- Offline-capable PWA.
- Privacy by default (no raw PII exposed).
- CI/CD with GitHub Actions.

## Stretch Goals (Post-MVP)
- Video upload → transcription → auto exercise extraction.
- Push notifications + weekly email recaps.
- Streaks, badges, XP gamification.
