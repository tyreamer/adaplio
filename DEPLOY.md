# 🚀 Deploying Adaplio to Vercel

## Quick Deploy Steps

### 1. Prerequisites
- ✅ Vercel account
- ✅ .NET 8 SDK installed (`dotnet --version` should show 8.0.xxx)
- ✅ Git configured

### 2. Deploy Options

#### Option A: Via Vercel Dashboard (Recommended)
1. Go to [vercel.com](https://vercel.com)
2. Click "Add New Project"
3. Import your GitHub repository
4. Vercel will auto-detect it's a .NET project
5. Override the **Output Directory** to: `src/Frontend/Adaplio.Frontend/bin/Release/net8.0/publish/wwwroot`
6. Deploy! 🎉

#### Option B: Via Vercel CLI
```bash
# Install Vercel CLI
npm install -g vercel

# Login to Vercel
vercel login

# Deploy from project root
vercel --prod
```

### 3. Custom Domain Setup
1. In Vercel Dashboard → Your Project → Settings → Domains
2. Add `adaplio.com` and `www.adaplio.com`
3. Update your DNS as instructed by Vercel

## What's Configured

✅ **SPA Routing:** All routes redirect to `/index.html` for client-side routing
✅ **Security Headers:** Modern security headers included
✅ **Caching:** Optimized caching for static assets and Blazor framework files
✅ **Build:** Native .NET support with `dotnet publish`

## Environment Variables

The app is configured to use:
- **API Base URL:** `https://adaplio.onrender.com`
- **Environment:** Production

No additional environment variables needed unless you want to override the API endpoint.

## Troubleshooting

- **Build fails:** Ensure .NET 8 SDK is installed
- **404 on routes:** Check that output directory is correctly set
- **API calls fail:** Verify the API base URL in `appsettings.Production.json`