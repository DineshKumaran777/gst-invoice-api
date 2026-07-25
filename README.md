# DK GST Billing Workspace

Enterprise GST billing workspace with a split architecture:

- GSTInvoice.API - ASP.NET Core 8 Web API (multi-tenant backend)
- ClientApp - React + TypeScript + Vite frontend
- GSTInvoice.Shared - shared DTOs, enums, pagination, and constants

## Solution

- `GSTInvoice.slnx`

## Product Vision

- See `docs/GST-Invoice-SaaS-V1-Product-Vision.md` for the V1 product vision, scope, UX principles, and delivery phasing.

## Prerequisites

- .NET SDK 8.0+
- Node.js 20+
- SQL Server LocalDB (or SQL Server)

## First-Time Setup

1. Restore packages:

	dotnet restore GSTInvoice.slnx

2. Install frontend dependencies:

	cd ClientApp

	npm install

3. Create or update API database (requires dotnet-ef installed):

	dotnet ef database update --project GSTInvoice.API/GSTInvoice.API.csproj --startup-project GSTInvoice.API/GSTInvoice.API.csproj

## Run Projects

Run API:

dotnet run --project GSTInvoice.API/GSTInvoice.API.csproj

Run React frontend:

cd ClientApp
npm run dev

## API Highlights

- JWT authentication with refresh tokens
- API versioned routes (`/api/v1/...`)
- Tenant resolution via claims and `X-Tenant-Id`
- Role and policy-ready authorization
- Request logging + global exception middleware
- FluentValidation + audit action filter
- Hangfire recurring jobs with dashboard (`/hangfire`)
- SignalR notifications hub (`/hubs/notifications`)
- Distributed cache abstraction (Redis or in-memory fallback)
- Swagger with Bearer auth support
- Health endpoint (`/health`)

## API Seed Data

Database seed creates:

- Roles: `SuperAdmin`, `CompanyAdmin`, `Staff`, `Viewer`
- Plans: `Free`, `Starter`, `Growth`, `Enterprise`
- Demo user: `demo@test.com` / `Demo@123`

## Configuration

Primary API config file:

- GSTInvoice.API/appsettings.json

Frontend app config:

- ClientApp/.env (if used)

Update JWT, SMTP, Twilio, Azure Blob, and CORS values before production deployment.
