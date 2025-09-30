# Adaplio - Physical Therapy Platform

## Overview
Adaplio is a comprehensive physical therapy platform built with ASP.NET Core Web API backend and Blazor WebAssembly frontend. The platform connects physical therapists with their clients, providing exercise management, progress tracking, gamification, and communication tools.

## Architecture

### Backend (ASP.NET Core Web API)
- **Location**: `src/Api/Adaplio.Api/`
- **Framework**: .NET 8 with ASP.NET Core
- **Database**: PostgreSQL (production) / SQLite (development) with Entity Framework Core 8.0
- **Authentication**: JWT Bearer tokens with magic link authentication for clients
- **Security**: Comprehensive middleware stack with rate limiting and audit logging
- **Communication**: Twilio SMS and MailKit email integration

### Frontend (Blazor WebAssembly)
- **Location**: `src/Frontend/Adaplio.Frontend/`
- **Framework**: Blazor WebAssembly .NET 8 with MudBlazor UI components
- **Styling**: Custom CSS design system with responsive layouts
- **State Management**: Scoped services and local component state
- **PWA Support**: Service worker and manifest for progressive web app capabilities

### Additional Projects
- **Jobs**: `src/Jobs/Adaplio.Jobs/` - Background job processing (placeholder)
- **Shared**: `src/Shared/Adaplio.Shared/` - Shared models and DTOs (placeholder)

## Technology Stack

### Backend Dependencies
- **Entity Framework Core 8.0** - Database ORM with PostgreSQL/SQLite providers
- **BCrypt.Net** - Secure password hashing
- **AspNetCoreRateLimit** - API rate limiting
- **Twilio** - SMS messaging service
- **MailKit** - Email service
- **QRCoder** - QR code generation
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT authentication

### Frontend Dependencies
- **MudBlazor 8.12.0** - Material Design UI components
- **Microsoft.AspNetCore.Components.WebAssembly 8.0** - Blazor framework
- **System.Net.Http.Json** - HTTP client extensions
- **Microsoft.Extensions.Http** - HTTP client factory

## Key Features

### 1. Authentication & Authorization System
- **Magic Link Authentication** for clients using 6-digit codes
- **Email/Password Authentication** for trainers with MFA support
- **JWT Token Security** with automatic refresh and secure storage
- **Role-Based Access Control** (Client/Trainer roles)
- **Comprehensive Audit Logging** for all security events

### 2. User Management
- **AppUser Base Entity** with specialized ClientProfile and TrainerProfile
- **Pseudonymous Client Aliases** (e.g., "C-7Q2F") for privacy
- **Consent Management System** between trainers and clients
- **Multi-Step Onboarding** workflows for both user types
- **Profile Management** with image upload and contact information

### 3. Exercise & Plan Management
- **Exercise Library** with categories, instructions, and default parameters
- **Plan Templates** created by trainers for reusability
- **Plan Proposals** with client acceptance workflow
- **Exercise Instances** with tracking and progress monitoring
- **Progress Logging** with automatic adherence calculations

### 4. Gamification System
- **XP-Based Leveling** with calculated progression curves
- **Streak Tracking** (daily and weekly exercise completion)
- **Badge System** with JSON storage for achievements
- **Progress Events** with automatic XP awards for milestones

### 5. Communication Features
- **Invite Token System** for secure client onboarding
- **SMS Notifications** via Twilio integration
- **Email Communications** via MailKit
- **QR Code Generation** for easy trainer-client connections

### 6. Modern User Interface
- **Responsive Design** optimized for mobile and desktop
- **Clean Landing Page** with user type selection
- **Dashboard Interfaces** for both trainers and clients
- **Exercise Detail Views** with completion tracking
- **Settings Management** with iOS-style grouped interface

## Directory Structure

```
src/
├── Api/
│   └── Adaplio.Api/
│       ├── Analytics/           # Usage analytics endpoints
│       ├── Auth/               # Authentication endpoints
│       ├── Data/               # Entity Framework context and models
│       ├── Middleware/         # Security and rate limiting
│       ├── Profile/            # User profile management
│       ├── Services/           # Business logic services
│       └── Program.cs          # Application entry point
├── Frontend/
│   └── Adaplio.Frontend/
│       ├── Components/         # Reusable UI components
│       │   ├── Base/          # Core components (ErrorBoundary, etc.)
│       │   ├── Profile/       # Profile management components
│       │   ├── Progress/      # Progress tracking components
│       │   └── Trainer/       # Trainer-specific components
│       ├── Extensions/         # Service extensions
│       ├── Pages/             # Razor pages
│       │   ├── Auth/          # Authentication pages
│       │   └── Profile/       # Profile pages
│       ├── Services/          # Frontend services
│       ├── Validators/        # Form validation
│       └── wwwroot/           # Static assets
├── Jobs/
│   └── Adaplio.Jobs/          # Background job processing (placeholder)
└── Shared/
    └── Adaplio.Shared/        # Shared models (placeholder)
```

## Database Schema

### Core Entities (20 Domain Models)

#### User Management
- **AppUser** - Base user entity with common properties
- **ClientProfile** - Client-specific data with pseudonymous aliases
- **TrainerProfile** - Trainer credentials and practice information
- **ConsentGrant** - Permission system between trainers and clients

#### Exercise Management
- **Exercise** - Exercise library with instructions and parameters
- **PlanTemplate** - Reusable exercise plans created by trainers
- **PlanProposal** - Plan proposals sent to clients for acceptance
- **PlanInstance** - Active client plans with tracking
- **ExerciseInstance** - Individual exercise assignments

#### Progress Tracking
- **ProgressEvent** - Exercise completion logs with timestamps
- **AdherenceWeek** - Weekly adherence statistics and summaries
- **PlanItemAcceptance** - Client acceptance tracking for plan items

#### Gamification
- **Gamification** - XP points, levels, streaks, and badges
- **XpAward** - XP awards tied to specific progress events

#### Authentication
- **MagicLink** - Magic link tokens for client authentication
- **GrantCode** - Trainer-generated codes for client invitations
- **InviteToken** - Secure invitation tokens

#### Media & Support
- **MediaAsset** - File storage references
- **Transcript** - Text transcriptions
- **ExtractionResult** - Data extraction results

## API Endpoints (46 total)

### Authentication Endpoints
```
POST /auth/client/magic-link     # Send magic link to client
POST /auth/client/verify         # Verify magic link token
POST /auth/trainer/register      # Trainer registration
POST /auth/trainer/login         # Trainer authentication
GET  /auth/me                    # Get current user information
PUT  /auth/profile               # Update user profile
POST /auth/role                  # Set user role
```

### Client Management
```
GET  /client/board               # Client dashboard data
GET  /client/progress            # Progress tracking data
GET  /client/plans               # Active client plans
GET  /client/proposals           # Plan proposals
GET  /client/gamification        # Gamification stats
```

### Trainer Management
```
GET  /trainer/clients            # Client list for trainer
GET  /trainer/templates          # Plan templates
POST /trainer/proposals          # Create plan proposals
GET  /trainer/grants            # Grant management
```

### Supporting Endpoints
```
POST /upload/presigned          # File upload with presigned URLs
GET  /health                    # Health check endpoint
POST /dev/seed                  # Development data seeding
GET  /analytics/events          # Usage analytics
```

## Frontend Pages & Components

### Main Pages
- **Home.razor** - Clean landing page with user type selection
- **Settings.razor** - iOS-style grouped settings interface
- **ActionPlans.razor** - PT action plan management with status tracking
- **ExerciseDetail.razor** - Detailed exercise view with completion tracking
- **HomeClient.razor** - Client dashboard with exercises and progress
- **HomeTrainer.razor** - Trainer dashboard with patient management
- **ClientProgress.razor** - Detailed progress tracking interface

### Authentication Pages
- **ClientLogin.razor** - Magic link authentication for clients
- **TrainerLogin.razor** - Email/password login for trainers
- **ClientOnboarding.razor** - Multi-step client registration
- **Verify.razor** - Magic link verification interface

### Components
- **ClientCard.razor** - Patient cards for trainer dashboard
- **ProgressLadder.razor** - Visual progress tracking
- **ErrorBoundary.razor** - Error handling component
- **ProfileHeader.razor** - User profile display
- **AccountSettings.razor** - Account management interface

## Security Architecture

### Middleware Stack
1. **SecurityRateLimitingMiddleware** - Prevents API abuse
2. **SecurityAuditMiddleware** - Comprehensive security logging
3. **ProfileRateLimitingMiddleware** - User-specific rate limits
4. **JWT Authentication** - Token validation with cookie fallback

### Rate Limiting Configuration
```json
{
  "Auth": "10 requests/minute",
  "General": "60 requests/minute",
  "Profile": "30 requests/minute"
}
```

### Security Features
- **Input Sanitization** for all user inputs
- **CORS Configuration** for production origins
- **Audit Logging** with detailed security events
- **Token Refresh** automatic JWT token management
- **Password Hashing** using BCrypt with salt

## Services Architecture

### Backend Services
- **IJwtService** - JWT token creation and validation
- **IEmailService** - Email communication via MailKit
- **ISMSService** - SMS messaging via Twilio
- **IAliasService** - Client pseudonym generation
- **IProgressService** - Exercise progress tracking
- **IPlanService** - Plan creation and management
- **IGamificationService** - XP and achievement system
- **IUploadService** - File upload and management
- **IAuditService** - Security event logging
- **ISecurityMonitoringService** - Threat detection

### Frontend Services
- **AuthStateService** - Authentication state management
- **ProfileService** - User profile operations
- **AuthenticatedHttpClient** - API communication with auth
- **NotificationService** - User notification system
- **FormValidationService** - Client-side form validation
- **ThemeService** - Dark/light mode management

## Development Commands

### Build and Run
```bash
# Build entire solution
dotnet build

# Build for production
dotnet build --configuration Release

# Run API (from src/Api/Adaplio.Api/)
dotnet run

# Run Frontend (from src/Frontend/Adaplio.Frontend/)
dotnet run
```

### Database Management
```bash
# Add migration
dotnet ef migrations add <MigrationName> --project src/Api/Adaplio.Api

# Update database
dotnet ef database update --project src/Api/Adaplio.Api

# Drop database
dotnet ef database drop --project src/Api/Adaplio.Api
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/Api/Adaplio.Api.Tests
```

## Environment Configuration

### Required Environment Variables
```bash
# Database Configuration
DB_PROVIDER=postgresql          # or sqlite for development
DB_CONNECTION=<connection_string>

# Authentication
JWT_SECRET=<secure_random_key>

# Communication Services
TWILIO_ACCOUNT_SID=<twilio_sid>
TWILIO_AUTH_TOKEN=<twilio_token>

# Email Configuration
SMTP_HOST=<smtp_server>
SMTP_PORT=<port>
SMTP_USERNAME=<username>
SMTP_PASSWORD=<password>

# CORS Configuration
CORS__ORIGINS=https://yourdomain.com,https://www.yourdomain.com
```

### Configuration Files
- **appsettings.json** - Base configuration
- **appsettings.Development.json** - Development overrides
- **appsettings.Production.json** - Production settings
- **render.yaml** - Render.com deployment configuration
- **vercel.json** - Vercel frontend deployment
- **Dockerfile** - Container configuration

## Design System

### Color Palette
- **Primary Orange**: #FF6B35 (brand primary)
- **Navy**: #1F2937 (dark text)
- **Cream Background**: #F9F7F4 (page backgrounds)
- **Success Green**: #10B981 (success states)
- **Error Red**: #EF4444 (error states)
- **Gray Scale**: #6B7280, #9CA3AF, #E5E7EB, #F3F4F6

### Typography
- **Font Family**: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif
- **Font Weights**: 400 (normal), 500 (medium), 600 (semibold), 700 (bold)
- **Font Sizes**: 14px, 16px, 18px, 20px, 24px, 28px, 32px, 48px

### Component Patterns
- **Border Radius**: 8px (buttons), 12px (cards), 16px (large cards)
- **Shadows**: Subtle elevation with rgba(0, 0, 0, 0.1)
- **Transitions**: 0.2s ease for hover states
- **Spacing Scale**: 4px, 8px, 12px, 16px, 20px, 24px, 32px, 40px

## Deployment

### Production Environment
- **API Hosting**: Render.com (Docker container)
- **Frontend Hosting**: Vercel (static deployment)
- **Database**: PostgreSQL on Render
- **Domain**: adaplio.com with SSL/TLS

### Build Pipeline
1. **API Build**: Docker multi-stage build with .NET 8
2. **Frontend Build**: Blazor WebAssembly publish
3. **Database Migration**: Automatic EF migrations on startup
4. **Health Checks**: Endpoint monitoring for uptime

### Monitoring & Logging
- **Health Endpoints**: `/health` for uptime monitoring
- **Security Audit Logs**: Comprehensive security event logging
- **Performance Monitoring**: Rate limiting and response time tracking
- **Error Tracking**: Detailed error logging with context

## Testing Structure

### Test Projects
- **Adaplio.Api.Tests** - API endpoint and service testing
- **Adaplio.Frontend.Tests** - Component and integration testing

### Test Categories
- **Unit Tests**: Service logic and data models
- **Integration Tests**: API endpoints with database
- **Component Tests**: Blazor component rendering
- **Security Tests**: Authentication and authorization
- **Performance Tests**: Load testing and optimization

## Recent Development Activity

### Major Features Completed (2024-2025)
1. **Complete UI Overhaul** - Modern responsive design across all pages
2. **Landing Page Redesign** - Clean welcome page with user type selection
3. **Dashboard Implementations** - Separate dashboards for trainers and clients
4. **Exercise Detail Pages** - Comprehensive exercise views with completion tracking
5. **Settings Interface** - iOS-style grouped settings with toggles
6. **Action Plan Management** - Table-based plan management with status tracking

### Recent Commits
- Frontend overhaul with modern UI components
- Route conflict resolution between settings pages
- CORS and deployment configuration fixes
- Exercise detail page implementation
- Authentication flow improvements

## Future Development Areas

### Planned Enhancements
- **Real-time Features** - WebSocket integration for live updates
- **Mobile App Development** - Native iOS/Android applications
- **Advanced Analytics** - Detailed progress reporting and insights
- **Wearable Integration** - Fitness tracker and smartwatch support
- **Video Exercise Library** - Video demonstrations and instructions
- **Telehealth Integration** - Virtual consultation capabilities

### Technical Improvements
- **Background Jobs** - Full implementation of job processing system
- **Caching Layer** - Redis for improved performance
- **API Versioning** - Version management for backward compatibility
- **Advanced Testing** - Comprehensive test coverage and automation
- **Performance Optimization** - Database query optimization and caching

## Code Conventions

### Backend Conventions
- **Controllers**: Minimal API endpoints with clear naming
- **Services**: Interface-based dependency injection
- **Models**: Entity Framework entities with proper relationships
- **Validation**: FluentValidation for complex business rules
- **Error Handling**: Consistent error responses with proper HTTP status codes

### Frontend Conventions
- **Components**: Razor components with code-behind separation
- **Services**: Scoped services for state management
- **Styling**: Component-scoped CSS with design system variables
- **Navigation**: Clean routing with proper parameter handling
- **State Management**: Reactive updates with StateHasChanged()

### Security Conventions
- **Authentication**: JWT tokens with secure storage
- **Authorization**: Role-based access control
- **Input Validation**: Server-side validation for all inputs
- **Audit Logging**: Comprehensive security event tracking
- **Rate Limiting**: Configurable limits per endpoint type

## Notes for Future Development

### Development Best Practices
1. **Follow existing naming patterns** and architectural decisions
2. **Maintain consistent component structure** across pages
3. **Use established color system** and design patterns
4. **Implement proper error handling** and loading states
5. **Add appropriate accessibility features** for inclusivity
6. **Include comprehensive logging** for debugging and monitoring

### Performance Considerations
- **Database Queries**: Use efficient queries with proper indexing
- **Frontend Rendering**: Implement virtualization for large lists
- **API Responses**: Paginate large datasets
- **Static Assets**: Optimize images and minimize bundle sizes
- **Caching**: Implement appropriate caching strategies

### Security Guidelines
- **Input Sanitization**: Validate and sanitize all user inputs
- **Authentication**: Secure token management and refresh cycles
- **Authorization**: Implement proper role-based access controls
- **Audit Trails**: Log all significant security events
- **Rate Limiting**: Protect against abuse and DOS attacks

This documentation represents the current state of the Adaplio platform as of January 2025, including all recent UI overhauls, feature implementations, and architectural improvements.