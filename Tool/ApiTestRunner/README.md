# ApiTestRunner

Runs a sequence of HTTP calls against the Auth API and records a log.

```powershell
# 1) start the API (Development) in another terminal
# 2) then, from this folder:
dotnet run
dotnet run -- --base-url https://localhost:5080 --stop-on-fail

Note: pay attention to the port, watch where AuthService is listening on
```

Output: `logs/run-<timestamp>.log` (readable) and `logs/run-<timestamp>.json` (machine-readable).
Exit code `0` = all passed, `1` = something failed, `2` = bad arguments/plan.

## How tests are defined
* `testplan/plan.json` – the ordered list of steps (comments allowed).
* `testplan/bodies/*.txt` – request body of each step (JSON text).

Step fields: `name`, `method`, `path`, `bodyFile`, `bearer`, `expectStatus`,
`expectBodyContains`, `expectBodyNotContains`, `capture`.

Variables use `{{name}}` in `path`, body files and `expectBody*`:
* built in: `{{runId}}` (7 random digits – makes usernames/emails/phones unique per run), `{{timestamp}}`
* captured: `"capture": { "aliceToken": "accessToken" }` stores `response.accessToken`
  (dotted paths such as `account.id` work); `"bearer": "aliceToken"` sends it as the JWT.

Notes: URL-encode `+` in query strings as `%2B`. Passwords and JWTs are masked in logs.
The plan relies on Development settings: seeded admin (`admin` / `Admin@12345`) and OTP codes echoed as `devCode`.
