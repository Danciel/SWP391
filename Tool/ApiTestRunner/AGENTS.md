# ApiTestRunner - guide for AI agents

You are working on **ApiTestRunner**, a .NET 8 console tool that runs an ordered list of HTTP calls
against the Auth API and checks the responses. Read this file fully before changing anything.
Humans use it from a browser UI; you will mostly edit `testplan/` files (or the C# for new features).

## Run it
```
dotnet run                                   # console run, uses testplan/plan.json
dotnet run -- --base-url http://localhost:5080 --stop-on-fail
dotnet run -- --ui [--port 5055] [--no-open] # browser UI (Postman/Swagger-like)
dotnet run -- --no-mask                      # show real passwords/tokens in logs (default follows plan.json maskSecrets)
```
Exit code: 0 = all passed, 1 = a step failed, 2 = bad arguments / unreadable plan.
The API under test must already be running. `baseUrl` in plan.json is only the default; `--base-url`
or the UI "Target" box overrides it. Check the port the API prints on startup (http vs https matters).

## Layout
```
ApiTestRunner/
  testplan/plan.json        <- THE test list (ordered steps)
  testplan/bodies/*.txt     <- JSON request bodies, one file per body
  logs/run-<stamp>.log|json <- output of every run (log = readable, json = used by the UI History tab)
  Program.cs                <- entry point: parses CLI, loads plan, runs console mode or UiServer
  CliOptions.cs             <- command-line flags
  Models.cs                 <- TestPlan / TestStep (plan schema), StepResult, CheckResult
  TestRunner.cs             <- executes steps, variable substitution, assertions, capture, logging
  Masker.cs                 <- hides passwords / long tokens in logs
  UiServer.cs               <- local web server for --ui (JSON API used by ui/index.html)
  ui/index.html             <- the whole front end (vanilla JS, no build step, embedded into the exe)
```

## plan.json schema
```json
{
  "baseUrl": "https://localhost:7080",
  "maskSecrets": false,                      // optional, default false = real passwords/JWTs in logs and UI (local dev). true = masked
  "steps": [
    {
      "name": "Login alice by username",     // required, unique-ish, shown everywhere
      "group": "Login",                      // optional, sidebar section in the UI
      "description": "free text note",       // optional, shown in UI
      "method": "POST",                      // GET POST PUT PATCH DELETE ... (default GET)
      "path": "/api/auth/login",             // path + query, {{variables}} allowed
      "bodyFile": "bodies/login-alice.txt",  // optional, relative to testplan/, {{variables}} allowed
      "bearer": "aliceToken",                // optional, NAME of a captured variable sent as Authorization: Bearer
      "expectStatus": 200,                   // default 200
      "expectBodyContains": ["\"tokenType\":\"Bearer\""],   // optional
      "expectBodyNotContains": ["passwordHash"],            // optional
      "capture": { "aliceToken": "accessToken", "aliceId": "account.id" }  // optional
    }
  ]
}
```
plan.json is read with comments and trailing commas allowed, property names are case-insensitive.
NOTE: saving from the UI rewrites the file without comments (a `plan.json.bak` is kept) - use `group` instead of comments.

## Rules the runner enforces (these cause most mistakes)
1. **Steps run top to bottom and share state.** A step that needs a token must come after the step that captures it.
   If an earlier step fails, later steps that use its variables fail with "Variable ... is not set".
2. **Variables**: `{{name}}` works in `path`, body files, and `expectBody*` strings. Built in: `{{runId}}`
   (7 random digits per run - use it in usernames/emails/phones so re-runs do not collide) and `{{timestamp}}`.
   An unknown variable fails the step ("Unresolved variable(s)").
3. **capture** maps `variableName -> dotted JSON path` in the response (`accessToken`, `account.id`).
   Object properties only (no array indexes). Captures happen only if the status matched `expectStatus`.
   A missing path fails the step.
4. **expectBodyContains / NotContains are plain, case-sensitive substring checks on the RAW response text.**
   ASP.NET returns compact JSON, so write `"role":"OWNER"` (no space after the colon). In plan.json, escape quotes:
   `"\"role\":\"OWNER\""`. Prefer several small checks over one long one.
5. Status is compared for equality with `expectStatus`. Redirects are NOT followed.
6. `+` in query strings must be written `%2B` (e.g. `phone=%2B84902{{runId}}`).
7. Body files are sent as `application/json`. Keep them valid JSON after substitution (quote string variables).
8. Masking: with `"maskSecrets": true` (or `--mask`) passwords become `***` and `accessToken` is shortened in logs/UI; with `false` (or `--no-mask`, or the UI checkbox) real values are shown. Only use throwaway dev credentials in body files.
9. If the server cannot be reached the run aborts ("Fatal") - that is a target/port problem, not a test problem.
10. The current plan assumes Development settings: seeded admin `admin` / `Admin@12345`, and OTP codes echoed back as `devCode`.

## How to ADD a test
1. Create the body (if the request has one): `testplan/bodies/<verb>-<what>.txt`, valid JSON, use `{{runId}}` for unique values.
2. Add a step object to `steps` in plan.json **at the right position** (after anything it depends on, e.g. after login
   if it uses `bearer`). Set `name`, `group`, `method`, `path`, `bodyFile`, `bearer`, `expectStatus`, and 1-3 `expectBodyContains`.
3. If later steps need a value from this response, add `capture` (and use `{{thatName}}` or `bearer: "thatName"` later).
4. Add negative cases next to the happy path (no token -> 401, wrong role -> 403, bad input -> 400, unknown id -> 404, duplicate -> 409).
5. Run `dotnet run -- --stop-on-fail` (API must be running) and confirm the new step PASSes. Read `logs/run-*.log` for details.

## How to MODIFY a test
- Change the expectation: edit `expectStatus` / `expectBodyContains` of that step.
- Change what is sent: edit its file in `testplan/bodies/` (note other steps may share the same file - grep `bodyFile` first,
  and create a new file if only one step should change).
- Rename/move a step: captured variable names are referenced by later steps (`bearer`, `{{...}}`) - keep them consistent.
- Delete a step: first check no later step uses its `capture` variables.
- After any change, run the whole plan, not just the edited step.

## Reading results
- `logs/run-*.log`: per step PASS/FAIL, request, status, response body, and `!` lines listing why it failed.
- `logs/run-*.json`: `steps[]` with `index, name, method, url, status, expectedStatus, passed, fatal, elapsedMs, problems,
  requestBody, responseBody, checks[], responseHeaders, captured`. (Older files use PascalCase keys.)
- A failing step usually means: wrong port/https, missing earlier capture, changed API contract, or spacing in a `contains` string.

## How to add a NEW kind of assertion or step option (C#)
1. `Models.cs`: add a property to `TestStep` (nullable, with an XML doc comment).
2. `TestRunner.cs` -> `ExecuteAsync`: evaluate it after the response is read, add `problems.Add(...)` when it fails
   **and** `checks.Add(new CheckResult("<kind>", "<text>", ok))` so the UI shows it.
3. `ui/index.html`: add an input in `renderEditor()`, save it in `editorInput()` (`data-f="yourField"` handles simple fields),
   and list it in the `checks` pane if you want a preview before the first run.
4. `UiServer.cs` -> `PUT /api/plan` normalises steps on save; add empty-value cleanup there if needed.
5. Document it in this file and in README.md. Keep CLI behaviour and exit codes unchanged.

## UI / server notes (only if you change them)
- `dotnet run -- --ui` serves `ui/index.html` (embedded resource; **rebuild after editing it**) on `http://localhost:5055`, bound to localhost only.
- API: `GET /api/plan`, `PUT /api/plan` (saves plan + body files), `POST /api/run` (streams one JSON object per line: `start`, `step`..., `done`),
  `POST /api/probe`, `GET /api/detect`, `GET /api/runs`, `GET /api/runs/{file}`, `GET /api/guide`.
  Non-GET calls require the header `X-ATR: 1`. Only `*.txt` files inside `testplan/` can be written.
- The UI keeps the target URL in browser localStorage; "Set default" writes it into plan.json.

## Password rules tests
Group "Password rules" probes the boundaries of the API's rule (currently reported as *8-100 chars, at least one letter and one digit*):
7 chars, no digit, no letter, 101 chars -> 400; 8 chars and 100 chars -> 201. If the rule changes, edit the `password` in the matching
`testplan/bodies/register-pw-*.txt` and the expected status. To probe another rule, copy one of those bodies + step (unique username via `{{runId}}`).

## Conventions
- Body file names: `<action>-<subject>[-<variant>].txt`, kebab-case. Step names: short, human, say what is expected
  ("Login with wrong password", not "test 6"). Always fill `group`.
- Never commit `bin/`, `obj/` or real credentials. Logs are safe to delete.
