#!/bin/bash
set -e

echo "🔧 Installing .NET 8..."
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0

echo "📝 Adding .NET to PATH..."
export PATH="$HOME/.dotnet:$PATH"

echo "✅ Verifying .NET installation..."
dotnet --version

echo "📦 Restoring NuGet packages..."
dotnet restore src/Frontend/Adaplio.Frontend/Adaplio.Frontend.csproj

echo "🏗️ Publishing Blazor WASM app..."
dotnet publish src/Frontend/Adaplio.Frontend/Adaplio.Frontend.csproj -c Release

echo "✅ Build completed successfully!"
echo "📁 Output directory contents:"
ls -la src/Frontend/Adaplio.Frontend/bin/Release/net8.0/publish/wwwroot/