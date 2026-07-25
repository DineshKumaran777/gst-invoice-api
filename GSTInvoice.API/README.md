# GSTInvoice.API

Enterprise backend API for GST invoice SaaS.

## Stack

- ASP.NET Core 8 Web API
- EF Core 8 + SQL Server
- ASP.NET Core Identity + JWT auth
- Hangfire (SQL Server storage)
- SignalR
- FluentValidation
- Serilog
- Swagger

## Project Path

- GSTInvoice.API

## Run

From repository root:

1. dotnet restore GSTInvoice.slnx
2. dotnet ef database update --project GSTInvoice.API/GSTInvoice.API.csproj --startup-project GSTInvoice.API/GSTInvoice.API.csproj
3. dotnet run --project GSTInvoice.API/GSTInvoice.API.csproj

## Endpoints

- Swagger: /swagger
- Health: /health
- Hangfire Dashboard: /hangfire
- SignalR Hub: /hubs/notifications

## API Route Pattern

- /api/v1/auth/*
- /api/v1/users/*
- /api/v1/clients/*
- /api/v1/products/*
- /api/v1/invoices/*
- /api/v1/invoice-items/*
- /api/v1/payments/*
- /api/v1/notifications/*
- /api/v1/reports/*
- /api/v1/settings/*

## Seeded Credentials

- demo@test.com / Demo@123

## Configuration

Set values in appsettings.json or appsettings.Development.json:

- ConnectionStrings:DefaultConnection
- ConnectionStrings:Redis (optional)
- Jwt:Issuer
- Jwt:Audience
- Jwt:SecretKey
- Email:* (SMTP)
- Twilio:*
- AzureBlob:*
- Razorpay:*
- Cors:AllowedOrigins
