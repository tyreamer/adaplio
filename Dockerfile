# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy only API-related project files for better caching
COPY src/Api/Adaplio.Api/*.csproj src/Api/Adaplio.Api/
COPY src/Shared/Adaplio.Shared/*.csproj src/Shared/Adaplio.Shared/

# Restore dependencies for API only
RUN dotnet restore src/Api/Adaplio.Api/Adaplio.Api.csproj

# Copy source code for API and shared libraries
COPY src/Api/ src/Api/
COPY src/Shared/ src/Shared/

# Build the API project
RUN dotnet publish src/Api/Adaplio.Api -c Release -o out

# Use the official .NET 8 runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the built app from the build stage
COPY --from=build /app/out .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Railway uses PORT environment variable
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}

# Expose port (Railway will use PORT env var)
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "Adaplio.Api.dll"]