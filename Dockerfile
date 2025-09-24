# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln .
COPY src/Api/Adaplio.Api/*.csproj src/Api/Adaplio.Api/
COPY src/Shared/Adaplio.Shared/*.csproj src/Shared/Adaplio.Shared/
COPY src/Jobs/Adaplio.Jobs/*.csproj src/Jobs/Adaplio.Jobs/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the API project
RUN dotnet publish src/Api/Adaplio.Api -c Release -o out

# Use the official .NET 8 runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install SQLite (if needed for migrations or tools)
RUN apt-get update && apt-get install -y sqlite3 && rm -rf /var/lib/apt/lists/*

# Copy the built app from the build stage
COPY --from=build /app/out .

# Create directory for SQLite database
RUN mkdir -p /app/data

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:$PORT
ENV DB_CONNECTION="Data Source=/app/data/adaplio.db"

# Expose the port that Render will use
EXPOSE $PORT

# Run the application
ENTRYPOINT ["dotnet", "Adaplio.Api.dll"]