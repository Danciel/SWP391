# User Secrets - only new value required

The uploaded project already has a stable `UserSecretsId`.
Existing Supabase, Gmail, and JWT values are not copied into ZIP files.

Add one new secret for Asset encryption.

## Generate a 32-byte KEK in PowerShell

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

Then from the folder containing `LegacyVault.csproj`:

```powershell
dotnet user-secrets set "Encryption:KekBase64" "PASTE_THE_GENERATED_BASE64_VALUE"
```

Do not share this value.
Do not commit it.
Do not reuse `Jwt:Key`.
