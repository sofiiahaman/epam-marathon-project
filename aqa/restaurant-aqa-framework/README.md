# Restaurant AQA Solution

Automated acceptance tests for the Green & Tasty restaurant application.

## Scope and approach

- **Direction 1:** group pre-order testing for the integrated backend and frontend.
- **Approach:** BDD + Screenplay using Reqnroll, TUnit, and Playwright for .NET.

Gherkin features describe business behavior. Screenplay tasks perform actions,
questions read results, API clients isolate HTTP details, and page objects isolate
UI locators and actions.

## Configuration

Create a local environment file from the template:

```bash
cp .env.example .env
```

Set `UI_URL` and `API_URL` to the deployment under test. Group pre-order and
reservation scenarios also require `TESTTABLE_ID` and a configured
`WAITER_USERNAME` / `WAITER_PASSWORD`. Optional admin scenarios use
`ADMIN_USERNAME` and `ADMIN_PASSWORD`.

The complete list is in [.env.example](.env.example). Environment variables
override `.env`. The file is git-ignored; never commit credentials or secrets.

## Running tests

Run from this directory:

```bash
dotnet build
dotnet run --no-build -- --report-trx
```

Run a subset by tag or level:

```bash
dotnet run --no-build -- --report-trx --treenode-filter "/*/*/*/*[Category=group]"
dotnet run --no-build -- --report-trx --treenode-filter "/*/*/*/*[Category=l1]"
dotnet run --no-build -- --report-trx --treenode-filter "/*/*/*/*[Category=l2]"
dotnet run --no-build -- --report-trx --treenode-filter "/*/*/*/*[Category=l3]"
dotnet run --no-build -- --report-trx --treenode-filter "/*/*/*/*[Category=ui]"
dotnet run --no-build -- --report-trx --treenode-filter "/*/*/*/*[Category=api]"
```

## Reports and artifacts

Test output is written to `TestResults/`:

- TUnit HTML and TRX reports;
- Reqnroll reports in `TestResults/reqnroll/`;
- Playwright traces in `TestResults/traces/`.

Traces include screenshots, DOM snapshots, network activity, and console output.
Open one with:

```bash
npx playwright show-trace TestResults/traces/<trace>.zip
```

`TRACE_MODE=OnFailureOnly` captures failed UI scenarios. `TRACE_MODE=Always`
also captures successful UI scenarios.

## Defined group pre-order behavior

The test suite records the choices made for behavior not fully specified by the
assignment:

- submitting an already submitted group returns `400`;
- a non-owner receives `403`;
- an unauthenticated request receives `401`;
- removing the last guest returns `participants: []` and `total: 0`;
- deleting the owner session returns `400`.

## CI

`.github/workflows/aqa-tests.yml` runs the suite nightly and on demand. It
supports `all`, `ui`, and `api` scopes plus a custom test filter.

The test step uses `continue-on-error: true`, so a failed test does not prevent
reports, summaries, and artifacts from being published. The workflow then marks
the job as failed when the test run failed, preserving the failure signal and
diagnostics.

See [CLAUDE.md](CLAUDE.md) for framework conventions and contribution guidance.
