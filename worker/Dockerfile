# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy remaining source code and publish
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Define entrypoint to run the console app
ENTRYPOINT ["dotnet", "worker.dll"]
