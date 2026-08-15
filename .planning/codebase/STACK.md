# Technology Stack

**Analysis Date:** 2026-07-23

## Languages

**Primary:**
- C# / .NET 10 - Backend API, services, persistence, email, and tests under `OpenRestoApi` and `OpenRestoApi.Tests`
- TypeScript 5.9 - Frontend routes, components, hooks, API client, and tests under `openresto-frontend`

**Secondary:**
- JavaScript - Frontend config/mocks/scripts
- Shell / Docker config - Nginx startup, container entrypoints, local infrastructure

## Runtime

**Environment:**
- .NET 10 SDK/runtime for the API and backend tests
- Node.js 20+ for frontend dev/test tooling
- Browser + React Native Web runtime for the public/admin UI

**Package Manager:**
- npm with `package-lock.json` at repo root and frontend root
- NuGet/MSBuild via `.csproj` and solution files for backend dependencies

## Frameworks

**Core:**
- ASP.NET Core 10 - HTTP API hosting and middleware composition in `OpenRestoApi/Program.cs`
- Entity Framework Core - SQLite persistence and migrations
- Expo Router / React Native 0.85 / React 19 - Shared web/mobile frontend

**Testing:**
- Jest + `jest-expo` + Testing Library - Frontend unit/component coverage
- Playwright - Frontend E2E coverage in `openresto-frontend/e2e`
- xUnit-style .NET test project - Backend unit/integration coverage in `OpenRestoApi.Tests`

**Build/Dev:**
- `concurrently` - Repo-level local dev orchestration
- Oxlint + Prettier - Frontend lint/format checks
- Docker Compose + Nginx - Local/full-stack runtime

## Key Dependencies

**Critical:**
- `expo-router` - Route structure for public/admin surfaces that need localization
- `react-native-web` - Shared rendering target for the browser experience
- MailKit / MimeKit - Outbound email seam that must become locale-aware
- EF Core + SQLite - Persistence model for restaurants, bookings, branding, and email settings
- ASP.NET Core authentication/rate limiting/problem details - Shared backend infrastructure that must keep current contracts stable

**Infrastructure:**
- Magick.NET - Dynamic brand/PWA image generation
- JWT bearer auth + cookies - Existing admin/session behavior

## Configuration

**Environment:**
- Backend uses `appsettings.json`, optional `appsettings.Local.json`, and environment variables such as `JWT_KEY`, `CONNECTION_STRING`, `CORS_ORIGINS`
- Frontend uses `EXPO_PUBLIC_API_URL`

**Build:**
- Root: `package.json`, `openresto.sln`, `Directory.Build.props`
- Frontend: `tsconfig.json`, `metro.config.js`, Expo config files
- Backend: `OpenRestoApi.csproj`, Dockerfile, migrations

## Platform Requirements

**Development:**
- Local Node.js + .NET SDK workflow or Docker Compose
- Cross-platform repo; no cloud service dependency required for core flows

**Production:**
- Dockerized deployment with backend, frontend, and Nginx/proxy layers
- SQLite-backed self-hosted runtime

---
*Stack analysis: 2026-07-23*
*Update after major dependency changes*
