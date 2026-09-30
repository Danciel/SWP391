# AuthService (.NET 8)

## Run (PostgreSQL in Docker)
```powershell
docker compose up -d          # starts postgres:16 -> db "legacyvault", user postgres / 12345, port 5432
dotnet restore
dotnet run                    # Development profile: Swagger at /swagger, tables auto-created
```

## Change JwtKey
Change your jwt key in appsettings.json
```cmd
"Jwt":{
    "Secret": "Enter your key here"
}
```

## Choose the database
Change prodider in appsettings.Development.json

`Database:Provider` = `Sqlite` | `Postgres` | `SqlServer` (connection string = `ConnectionStrings:<Provider>`).
Development defaults to Postgres. For SQL Server Express set in appsettings.Development.json:
```json
"Database": { "Provider": "SqlServer" }
```
(connection string for `LAPTOP-DA94HUFJ\SQLEXPRESS`, db `legacyvault`, user `sa` is already there; SQL auth must be enabled.)

## Generate test users
```powershell
dotnet run -- --seed-users 30     # Development only; idempotent
```
Creates `testadmin` + `testuser001..030` (password `Test@12345`), cycling the 6 non-admin roles and
5 verification states (unverified / email / email+phone / identity VERIFIED / identity REJECTED).

## Dev-only settings (appsettings.Development.json)
Seeded admin `admin` / `Admin@12345`, OTP codes echoed as `devCode`, rate limit raised to 1000/min.
For non-dev set `Jwt__Secret` (>= 32 chars) and the connection string via environment variables.
