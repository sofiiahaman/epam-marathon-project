# Green & Tasty Restaurant

Green & Tasty is a restaurant reservation and pre-order application. Customers
can discover dishes, book tables, prepare orders, invite guests to a group
reservation, and submit one consolidated group pre-order.

## Product capabilities

- customer, waiter, and admin workflows;
- restaurant locations, tables, dishes, and menu browsing;
- reservations and reservation feedback;
- customer and waiter pre-orders;
- group pre-order sessions with participant-level dishes and totals;
- owner-only group submission and participant management;
- API, frontend, and acceptance-test coverage.

## Repository structure

| Directory | Description |
|---|---|
| `backend/restaurant-backend-app` | AWS Lambda API and backend tests |
| `frontend/restaurant-frontend-app` | Angular web application |
| `aqa/restaurant-aqa-framework` | Reqnroll + TUnit + Playwright acceptance tests |
| `infra/terraform` | Infrastructure as code |

## Documentation

- [Backend](backend/restaurant-backend-app/README.md)
- [Frontend](frontend/restaurant-frontend-app/README.md)
- [AQA solution](aqa/restaurant-aqa-framework/README.md)
- [API routes](backend/restaurant-backend-app/docs/ApiRoutes.md)

## Technology

- Angular and TypeScript;
- .NET and AWS Lambda;
- Amazon API Gateway, Cognito, DynamoDB, S3, and CloudFront;
- Terraform and AWS Syndicate;
- Reqnroll, TUnit, and Playwright.
