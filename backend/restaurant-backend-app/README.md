# Restaurant Backend

AWS Lambda backend for the Green & Tasty restaurant application.

The API handles authentication, reservations, menus, orders, feedback, and
group pre-order sessions. Route details are documented in
[docs/ApiRoutes.md](docs/ApiRoutes.md).

## Project layout

- `dnapp/lambdas/api-handler` — API handlers, services, repositories, and models;
- `dnapp/lambdas/api-handler.Tests` — unit and integration tests;
- `swagger-endpoints.yaml` — API endpoint reference.

## Validation

From `backend/restaurant-backend-app`:

```bash
dotnet build
dotnet test
```
