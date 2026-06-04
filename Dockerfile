# Stage 1: Build frontend
FROM node:20-alpine AS frontend-build
WORKDIR /src/ui
COPY ui/package.json ui/package-lock.json* ./
RUN npm install
COPY ui/ ./
RUN npm run build-only

# Stage 2: Build backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY DownloadCenter.sln ./
COPY src/DownloadCenter.Api/DownloadCenter.Api.csproj src/DownloadCenter.Api/
COPY src/DownloadCenter.Application/DownloadCenter.Application.csproj src/DownloadCenter.Application/
COPY src/DownloadCenter.Domain/DownloadCenter.Domain.csproj src/DownloadCenter.Domain/
COPY src/DownloadCenter.Infrastructure/DownloadCenter.Infrastructure.csproj src/DownloadCenter.Infrastructure/
COPY src/DownloadCenter.Shared/DownloadCenter.Shared.csproj src/DownloadCenter.Shared/
RUN dotnet restore
COPY src/ src/
RUN dotnet publish src/DownloadCenter.Api/DownloadCenter.Api.csproj -c Release -o /app/publish --no-restore

# Copy frontend dist to wwwroot
COPY --from=frontend-build /src/ui/dist /app/publish/wwwroot

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=backend-build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "DownloadCenter.Api.dll"]
