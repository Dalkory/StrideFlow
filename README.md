# StrideFlow

StrideFlow is a monolithic ASP.NET 8 + React pedometer platform focused on clean architecture, production-ready JWT auth, live SignalR updates, PostgreSQL persistence, Redis-backed realtime state, and a polished dashboard experience.

## Product highlights

- JWT access tokens with refresh-token rotation and logout revocation.
- Live walking sessions with route plotting on a real map.
- Daily, weekly, and monthly leaderboards with city filtering.
- Telegram Stars reward preview for weekly and monthly top performers.
- Reward Center API with weekly/monthly city standing, payout eligibility, and test-mode settlement status.
- Smart Coach insights with daily action plan, consistency score, and achievement progress.
- Placeholder ad inventory exposed through the API for future sponsor insertion.
- React dashboard with live map, history, profile management, and demo-mode tracking for desktop showcases.
- Integration tests covering API endpoints, SignalR negotiation, validation, and domain error paths with PostgreSQL and Redis containers.

## Architecture

- `src/StrideFlow.Domain`: domain entities and invariants.
- `src/StrideFlow.Application`: contracts, DTOs, validation, configuration models.
- `src/StrideFlow.Infrastructure`: EF Core, Redis live-session store, auth and application services.
- `src/StrideFlow.Api`: controllers, SignalR hub, middleware, SPA hosting, and Frame-style startup definitions.
- `src/StrideFlow.ClientApp`: Vite + React frontend.
- `tests/StrideFlow.IntegrationTests`: end-to-end API coverage using Testcontainers.

Runtime composition follows a definition-based approach: `Program.cs` stays minimal, while logging, database migrations, web pipeline, JWT auth, SignalR, Swagger, CORS, rate limiting, endpoints, and SPA hosting live in focused classes under `src/StrideFlow.Api/Definitions`.

## Local development

1. Start infrastructure:

```bash
docker compose up -d postgres redis
```

2. Run the API:

```bash
dotnet run --project src/StrideFlow.Api
```

3. Run the frontend in dev mode:

```bash
cd src/StrideFlow.ClientApp
npm install
npm run dev
```

The Vite dev server proxies `/api`, `/hubs`, and `/health` to `http://localhost:5268` by default. Override the target with `VITE_API_TARGET` if needed.

## Production-style local run

```bash
docker compose up --build
```

The containerized app is exposed on `http://localhost:18080`.

## Tests and quality checks

Backend integration tests:

```bash
dotnet test StrideFlow.sln
```

Frontend checks:

```bash
cd src/StrideFlow.ClientApp
npm run lint
npm run build
```

## Notes

- The backend now applies EF Core migrations on startup instead of using `EnsureCreated`.
- The frontend includes a desktop-friendly `Demo route` mode so the product can be showcased without mobile GPS access.
- OpenStreetMap tiles are used for map rendering in the browser.
