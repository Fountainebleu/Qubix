# Qubix

Qubix is a web application for creating and running real-time quizzes.

## Project structure

- `backend/Qubix.Api` — ASP.NET Core Web API.
- `backend/Qubix.Core` — domain model and application rules.
- `backend/Qubix.Infrastructure` — database and external service integrations.
- `backend/Qubix.UnitTests` — unit tests.
- `backend/Qubix.IntegrationTests` — integration tests.
- `frontend` — React, TypeScript, and Vite client.

## Requirements

- .NET SDK 10
- Node.js 24 or newer
- npm 11 or newer

## Build

Build and test the backend:

```powershell
dotnet build Qubix.sln
dotnet test Qubix.sln --no-build
```

Install dependencies and build the frontend:

```powershell
cd frontend
npm install
npm run build
```

## Development

Start the backend:

```powershell
dotnet run --project backend/Qubix.Api
```

Start the frontend in another terminal:

```powershell
cd frontend
npm run dev
```
