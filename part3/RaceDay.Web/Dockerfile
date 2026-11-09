# syntax=docker/dockerfile:1

# ---- Build stage: restore, compile and publish with the full SDK ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first so the restore layer is cached until dependencies change.
COPY RaceDay.Web.csproj ./
RUN dotnet restore RaceDay.Web.csproj

COPY . ./
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime stage: small ASP.NET image with only what the app needs ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish ./

# Run as the non-root user that ships with the .NET images.
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
