# Adaplio - Physical Therapy Platform

## Overview
Adaplio is a comprehensive physical therapy platform built with ASP.NET Core Web API backend and Blazor WebAssembly frontend. The platform connects physical therapists with their clients, providing exercise management, progress tracking, and communication tools.

## Architecture

### Backend (ASP.NET Core Web API)
- **Location**: `src/Api/Adaplio.Api/`
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT-based with magic links
- **Security**: Rate limiting, audit logging, CORS configuration
- **Communication**: Twilio SMS integration

### Frontend (Blazor WebAssembly)
- **Location**: `src/Frontend/Adaplio.Frontend/`
- **Framework**: Blazor WebAssembly with MudBlazor components
- **Styling**: Custom CSS with design system
- **State Management**: Scoped services and local state

## Key Features Implemented

### 1. Authentication System
- **Magic Link Authentication** for both trainers and clients
- **Multi-step onboarding** with profile setup
- **Role-based access control** (Trainer/Client)
- **Secure JWT token management**

### 2. User Interface Overhauls
Recent major UI overhauls completed:

#### Settings Page (`Pages/Settings.razor`)
- **iOS-style grouped settings** with Account, Notifications, App Preferences, and About sections
- **Custom toggle switches** with orange accent color
- **Clean navigation** and responsive design

#### Landing Page (`Pages/Home.razor`)
- **Modern hero section** with custom CSS exercise illustration
- **"How Adaplio Works"** feature section
- **Responsive navigation** header
- **Custom CSS animations** for person exercising with weights

#### Trainer Dashboard (`Pages/HomeTrainer.razor`)
- **Patient management interface** with card-based layout
- **Patient filtering** and search functionality
- **Progress tracking** with adherence percentages
- **Invite modal** for new patient onboarding

#### Action Plans Page (`Pages/ActionPlans.razor`)
- **Clean table design** for PT action plan management
- **Status badges** (Draft, Sent, Approved, Rejected) with color coding
- **Action buttons** for Edit, Send, and Delete operations
- **"New Invite" modal** with form validation

#### Exercise Detail Page (`Pages/ExerciseDetail.razor`)
- **Full exercise view** with video player placeholder
- **Exercise parameters** in colored cards (Sets, Hold, Rest)
- **Instructions and cautions** sections
- **Completion tracking** with "Mark as complete" functionality
- **Difficulty reporting** for trainer feedback

### 3. Client Features
#### Client Dashboard (`Pages/HomeClient.razor`)
- **Today's exercises** with progress tracking
- **XP progression system** with level indicators
- **Streak tracking** with visual indicators
- **Trainer notes** and communication
- **Clickable exercise cards** linking to detail pages

### 4. Design System
Consistent branding throughout the application:
- **Primary Orange**: #FF6B35
- **Navy**: #1F2937
- **Cream Background**: #F9F7F4
- **Success Green**: #10B981
- **Error Red**: #EF4444

## Directory Structure

```
src/
├── Api/
│   └── Adaplio.Api/
│       ├── Auth/                 # Authentication endpoints
│       ├── Services/             # Business logic services
│       ├── Middleware/           # Security and rate limiting
│       └── Program.cs            # API configuration
├── Frontend/
│   └── Adaplio.Frontend/
│       ├── Pages/                # Razor pages
│       │   ├── Home.razor        # Landing page
│       │   ├── Settings.razor    # User settings
│       │   ├── HomeClient.razor  # Client dashboard
│       │   ├── HomeTrainer.razor # Trainer dashboard
│       │   ├── ActionPlans.razor # PT action plans
│       │   ├── ExerciseDetail.razor # Exercise details
│       │   └── Auth/             # Authentication pages
│       ├── Components/           # Reusable components
│       │   ├── Profile/          # Profile management
│       │   └── Trainer/          # Trainer-specific components
│       └── Services/             # Frontend services
├── Shared/
│   └── Adaplio.Shared/           # Shared models and DTOs
└── Jobs/
    └── Adaplio.Jobs/             # Background job processing
```

## Development Commands

### Build and Run
```bash
# Build entire solution
dotnet build

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
```

## Environment Configuration

### Required Environment Variables
- `DB_PROVIDER`: Database provider (postgresql/sqlite)
- `DB_CONNECTION`: Database connection string
- `TWILIO_ACCOUNT_SID`: Twilio SMS service
- `TWILIO_AUTH_TOKEN`: Twilio authentication
- `JWT_SECRET`: JWT signing key

### CORS Configuration
Configured for production with proper origins in `render.yaml`:
```yaml
- key: CORS_ORIGINS
  value: "https://adaplio.onrender.com,https://www.adaplio.com"
```

## Key Technical Patterns

### 1. CSS in Blazor
- Using `@@keyframes` and `@@media` for CSS rules (escaped for Blazor compilation)
- Custom CSS animations and transitions
- Responsive design with mobile-first approach

### 2. Navigation Patterns
- Consistent header design across pages
- Role-based navigation (Client vs Trainer views)
- Breadcrumb and back navigation where appropriate

### 3. State Management
- Scoped services for user state
- Local component state for UI interactions
- Event callbacks for parent-child communication

### 4. Form Validation
- DataAnnotations validation
- Custom validation messages
- Loading states and error handling

## Security Features

### 1. Rate Limiting
- API endpoint rate limiting
- Security-focused rate limiting middleware
- Profile-specific rate limiting

### 2. Audit Logging
- Comprehensive audit trail
- Security monitoring service
- User action logging

### 3. Authentication Security
- Magic link expiration
- JWT token management
- Role-based authorization

## Recent Completions

### UI/UX Overhauls (2024)
1. ✅ **Settings Page** - iOS-style grouped settings with toggles
2. ✅ **Landing Page** - Modern hero with custom CSS illustration
3. ✅ **Trainer Dashboard** - Patient management with cards and filtering
4. ✅ **Action Plans Page** - Clean table design with status badges
5. ✅ **Exercise Detail Page** - Full exercise view with completion tracking

### Technical Improvements
1. ✅ **CORS Configuration** - Production-ready CORS setup
2. ✅ **Responsive Design** - Mobile-optimized across all pages
3. ✅ **Component Navigation** - Clickable exercise cards with routing
4. ✅ **Loading States** - Skeleton loading and spinner animations
5. ✅ **Error Handling** - Comprehensive error boundaries and validation

## Future Development Areas

### Potential Enhancements
- Real video/image upload for exercises
- Advanced progress analytics
- Real-time messaging between trainers and clients
- Mobile app development
- Integration with wearable devices
- Advanced reporting and analytics

### Technical Debt
- Migrate from mock data to real API endpoints
- Implement comprehensive unit testing
- Add integration testing
- Performance optimization
- Enhanced accessibility features

## Deployment

### Production Environment
- **Platform**: Render.com
- **Database**: PostgreSQL
- **Frontend**: Static deployment
- **API**: Container deployment

### Environment Setup
1. Configure environment variables in Render dashboard
2. Deploy API and Frontend separately
3. Ensure CORS origins match production domains
4. Verify database connectivity

## Notes for Future Development

### Code Conventions
- Follow existing naming patterns
- Maintain consistent component structure
- Use established color system
- Implement responsive design patterns
- Follow security best practices

### Component Development
- Check existing components before creating new ones
- Follow established CSS patterns
- Use proper error handling
- Implement loading states
- Add appropriate accessibility features

### API Development
- Follow existing endpoint patterns
- Implement proper error handling
- Add rate limiting where appropriate
- Include audit logging for sensitive operations
- Follow security middleware patterns