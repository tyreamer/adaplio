#!/bin/bash
set -e

echo "🔧 Installing .NET 8..."
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
export PATH="$HOME/.dotnet:$PATH"

echo "📦 Restoring NuGet packages..."
dotnet restore src/Frontend/Adaplio.Frontend/Adaplio.Frontend.csproj

echo "🏗️ Building Blazor WASM app..."
dotnet publish src/Frontend/Adaplio.Frontend/Adaplio.Frontend.csproj -c Release -o dist --nologo

echo "✅ Build completed successfully!"
ls -la dist/wwwroot/