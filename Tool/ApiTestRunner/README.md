# ApiTestRunner

Runs a sequence of HTTP calls against the Auth API, checks the responses, and records a log.
Two ways to use it: the **browser UI** (recommended for humans) or the **console**.

## Browser UI (Postman / Swagger style)
```powershell
# 1) start the API (Development) in another terminal - note the port it prints
# 2) from this folder:
dotnet run -- --ui          # or double-click start-ui.cmd
```
Opens http://localhost:5055 with:
* **Target** box - change which localhost/port is tested. Green dot = something answers there.
  *Test* checks it, *Detect* scans common local ports, *Set default* saves it into plan.json.
* **Run all** / **Run up to here**, live pass/fail per step, stop button, "stop on first failure".
* Per test: pretty response, headers, request sent, every check (with the reason it failed), captured variables, copy as cURL.
* **Edit test** tab: add / edit / reorder / duplicate / delete steps and body files without touching JSON (Ctrl+S saves).
* **History** tab: browse earlier `logs/run-*.json`.
* **Copy agent guide**: copies `AGENTS.md` so you can paste it into an AI agent.

The UI listens on localhost only. Another port: `dotnet run -- --ui --port 5056`.

## Console
```powershell
dotnet run
dotnet run -- --base-url https://localhost:5080 --stop-on-fail
```
Note: pay attention to the port, watch where AuthService is listening on.

Output: `logs/run-<timestamp>.log` (readable) and `logs/run-<timestamp>.json` (machine-readable).
Exit code `0` = all passed, `1` = something failed, `2` = bad arguments/plan.

## How tests are defined
* `testplan/plan.json` - the ordered list of steps.
* `testplan/bodies/*.txt` - request body of each step (JSON text).

Step fields: `name`, `group`, `description`, `method`, `path`, `bodyFile`, `bearer`, `expectStatus`,
`expectBodyContains`, `expectBodyNotContains`, `capture`. Full rules and recipes: **AGENTS.md**.

Variables use `{{name}}` in `path`, body files and `expectBody*`:
* built in: `{{runId}}` (7 random digits - makes usernames/emails/phones unique per run), `{{timestamp}}`
* captured: `"capture": { "aliceToken": "accessToken" }` stores `response.accessToken`
  (dotted paths such as `account.id` work); `"bearer": "aliceToken"` sends it as the JWT.

Notes: URL-encode `+` in query strings as `%2B`. Masking of passwords/JWTs is controlled by `"maskSecrets"` in plan.json (currently `false` = real values shown, for local development), the `--mask` / `--no-mask` flags, or the "mask passwords/tokens" checkbox in the UI.
The plan relies on Development settings: seeded admin (`admin` / `Admin@12345`) and OTP codes echoed as `devCode`.
Saving from the UI rewrites plan.json without comments (a `plan.json.bak` is kept) - use `group` for sections.
