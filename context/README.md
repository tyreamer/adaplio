# Adaplio
[adaplio.com](https://adaplio.com)

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
```bash
# Clone repo
git clone https://github.com/YOURNAME/adaplio.git
cd adaplio

# Launch local services
docker compose up -d

# Run API (SQLite)
cd src/Api
dotnet run

# Run Frontend
cd ../Frontend
dotnet run
