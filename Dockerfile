# Building
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /source

# Add project
COPY *.csproj ./
RUN dotnet restore

# Add source folders
ADD Authentication/ Authentication/
ADD Controllers/ Controllers/
ADD Database/ Database/
ADD Models/ Models/
ADD Properties/ Properties/
ADD Services/ Services/

# Add configurations
COPY *.json ./

# Add start files
COPY *.cs ./

# Build
RUN dotnet publish -c Production -o /app --no-restore

# Deploy
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

# AWS Credentials
WORKDIR /app
COPY --from=build /app .

# Expose default port
EXPOSE 80

ENTRYPOINT ["dotnet", "patrons-web-api.dll"] 