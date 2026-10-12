# Build stage: restore and publish with the full SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, so the restore layer is cached until a dependency changes.
COPY Sahaf.slnx global.json ./
COPY src/Sahaf.Models/Sahaf.Models.csproj src/Sahaf.Models/
COPY src/Sahaf.Utility/Sahaf.Utility.csproj src/Sahaf.Utility/
COPY src/Sahaf.DataAccess/Sahaf.DataAccess.csproj src/Sahaf.DataAccess/
COPY src/Sahaf.Web/Sahaf.Web.csproj src/Sahaf.Web/
RUN dotnet restore src/Sahaf.Web/Sahaf.Web.csproj

COPY src/ src/
RUN dotnet publish src/Sahaf.Web/Sahaf.Web.csproj -c Release -o /app --no-restore /p:UseAppHost=false

# Runtime stage: only the ASP.NET runtime, running as the image's non-root "app" user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production \
    # Workstation GC keeps memory low on a 512 MB instance.
    DOTNET_gcServer=0
USER $APP_UID
EXPOSE 8080

# Render passes the port to listen on in $PORT.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Sahaf.Web.dll"]
