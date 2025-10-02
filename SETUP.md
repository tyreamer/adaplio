# Adaplio Setup Guide

## Email Service Configuration

### Production (Resend API)

The application uses [Resend](https://resend.com) for email delivery in production.

#### Required Environment Variables

Set these on your hosting platform (Render, Railway, etc.):

```bash
# Required for email to work
RESEND_API_KEY=re_xxxxxxxxxxxxxxxxxxxx
RESEND_FROM_EMAIL=noreply@adaplio.com
```

#### Getting a Resend API Key

1. Sign up at [resend.com](https://resend.com)
2. Verify your sending domain (or use their test domain)
3. Generate an API key from the dashboard
4. Add the key to your production environment variables

### Development (Console Logging)

For local development, the email service automatically falls back to console logging when `RESEND_API_KEY` is not set.

**Magic link codes will appear in the API console output:**

```
=== MAGIC LINK CODE for user@example.com ===
CODE: 123456
=========================================
```

You can then use this code in the verification endpoint or frontend form.

### Development (MailHog - Optional)

For testing actual email delivery locally, you can use MailHog:

```bash
# Start MailHog with Docker
docker compose up -d

# MailHog UI will be available at:
# http://localhost:8025
```

**Note**: The current EmailService implementation uses Resend API, not SMTP. To use MailHog, you would need to implement the SMTP configuration that's defined in `appsettings.json`.

## Database Configuration

### Local Development (SQLite)

No configuration needed - SQLite database is created automatically on first run.

```bash
# Database file location:
# src/Api/Adaplio.Api/db.sqlite
```

### Production (PostgreSQL)

Set the database environment variables:

```bash
DB_PROVIDER=pgsql
DB_CONNECTION=Host=your-db.supabase.co;Database=postgres;Username=postgres;Password=xxxxx
```

## JWT Configuration

### Development

The default JWT secret in `appsettings.json` works for local development.

### Production

**IMPORTANT**: Set a strong, unique JWT secret:

```bash
JWT_SECRET=your-very-long-random-secret-key-at-least-32-characters-long
```

Generate a secure secret:
```bash
# Using openssl
openssl rand -base64 32

# Using Node.js
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"

# Using Python
python -c "import secrets; print(secrets.token_urlsafe(32))"
```

## CORS Configuration

### Development

Local development uses default CORS origins (localhost:5000, localhost:5001).

### Production

Set allowed frontend origins:

```bash
CORS__ORIGINS=https://adaplio.com,https://www.adaplio.com,https://adaplio.vercel.app
```

## Frontend Configuration

### API Base URL

The frontend needs to know where the API is hosted.

#### Development

Update `src/Frontend/Adaplio.Frontend/wwwroot/appsettings.Development.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

#### Production

Update `src/Frontend/Adaplio.Frontend/wwwroot/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://adaplio.onrender.com"
  }
}
```

## Complete Environment Variable Reference

### Backend API (Required)

```bash
# Database
DB_PROVIDER=pgsql              # or "sqlite" for local dev
DB_CONNECTION=<connection_string>

# JWT
JWT_SECRET=<secure_random_key>

# Email (Production)
RESEND_API_KEY=<resend_api_key>
RESEND_FROM_EMAIL=noreply@adaplio.com

# CORS
CORS__ORIGINS=https://yourfrontend.com,https://www.yourfrontend.com
```

### Backend API (Optional)

```bash
# Port (defaults to 8080)
PORT=8080

# Skip database migrations (for troubleshooting)
SKIP_MIGRATIONS=true
SKIP_COLUMN_FIXES=true

# JWT Configuration (has defaults)
JWT_ISSUER=adaplio-api
JWT_AUDIENCE=adaplio-frontend
```

## Deployment Checklist

### Before Deploying

- [ ] Generate strong JWT_SECRET
- [ ] Set up Resend account and get API key
- [ ] Configure database (PostgreSQL recommended)
- [ ] Set CORS origins to match frontend domain
- [ ] Update frontend API base URL to production API

### After Deploying

- [ ] Test magic link email delivery
- [ ] Test trainer registration and login
- [ ] Verify JWT tokens are working
- [ ] Check database migrations ran successfully
- [ ] Test CORS from frontend domain

## Troubleshooting

### "Magic link code not received"

**Symptom**: Client doesn't receive magic link email

**Solutions**:
1. Check `RESEND_API_KEY` is set correctly
2. Check API console logs for the code (dev fallback)
3. Verify Resend domain is verified
4. Check email isn't in spam folder

### "API connection failed"

**Symptom**: Frontend can't connect to API

**Solutions**:
1. Check `appsettings.json` has correct API URL
2. Verify CORS origins include frontend domain
3. Check API is running and accessible
4. Look for browser console errors

### "Database error on startup"

**Symptom**: API fails to start with database errors

**Solutions**:
1. Set `SKIP_MIGRATIONS=true` temporarily
2. Check database connection string
3. Verify database user has sufficient permissions
4. Check PostgreSQL logs for specific errors

### "JWT token invalid"

**Symptom**: Authentication fails after login

**Solutions**:
1. Check JWT_SECRET matches between API instances
2. Verify JWT_ISSUER and JWT_AUDIENCE match config
3. Clear browser cookies/localStorage
4. Check token expiration (24 hours default)

## Quick Start Commands

```bash
# Clone and setup
git clone <repo>
cd adaplio

# Install dependencies
dotnet restore

# Run API (terminal 1)
cd src/Api/Adaplio.Api
dotnet run

# Run Frontend (terminal 2)
cd src/Frontend/Adaplio.Frontend
dotnet run

# Access application
# Frontend: http://localhost:5000
# API: http://localhost:8080
# Magic link codes will appear in API terminal
```
