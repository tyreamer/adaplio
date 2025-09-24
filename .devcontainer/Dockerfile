FROM mcr.microsoft.com/devcontainers/dotnet:8.0

# Install Node.js for PWA development
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs

# Install additional tools
RUN apt-get update \
    && apt-get install -y git curl wget \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Set up user
USER vscode

# Install global dotnet tools
RUN dotnet tool install --global dotnet-ef