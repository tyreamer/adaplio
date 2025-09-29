# Vercel Deployment Script for Adaplio
Write-Host "🚀 Deploying Adaplio to Vercel..." -ForegroundColor Green

# Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "dist") {
    Remove-Item -Recurse -Force "dist"
}

# Build the project
Write-Host "🔨 Building Blazor WASM project..." -ForegroundColor Yellow
dotnet publish src/Frontend/Adaplio.Frontend/Adaplio.Frontend.csproj -c Release -o dist --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build completed successfully!" -ForegroundColor Green

# Check if Vercel CLI is installed
if (!(Get-Command "vercel" -ErrorAction SilentlyContinue)) {
    Write-Host "📦 Installing Vercel CLI..." -ForegroundColor Yellow
    npm install -g vercel
}

# Deploy to Vercel
Write-Host "🌐 Deploying to Vercel..." -ForegroundColor Yellow
vercel --prod

Write-Host "🎉 Deployment complete!" -ForegroundColor Green
Write-Host "Your app should be available at your Vercel domain" -ForegroundColor Cyan