# Use Ubuntu base with .NET SDK to build and run
FROM mcr.microsof.tcom/dotnet/sdk:8.0 AS build-env

# Install LibreOffice
RUN apt-get update && apt-get install -y \
    libreoffice \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy project files and restore
COPY ["FormsLetters/FormsLetters.csproj", "FormsLetters/"]
RUN dotnet restore "FormsLetters/FormsLetters.csproj"

# Copy all source
COPY . .

# Build and publish
WORKDIR /app/FormsLetters
RUN dotnet publish "FormsLetters.csproj" -c Release -o /out

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install LibreOffice again in runtime container
RUN apt-get update && apt-get install -y \
    libreoffice \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build-env /out .

ENTRYPOINT ["dotnet", "FormsLetters.dll"]
