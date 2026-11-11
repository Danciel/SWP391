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

## Google Drive Picker (asset verification)

The Drive flow now lets the owner pick the file in Google Picker after OAuth
(`POST /api/assets/connections/google_drive/picker-session`). Add:

```powershell
dotnet user-secrets set "OAuth:GOOGLE_DRIVE:PickerApiKey" "YOUR_BROWSER_API_KEY"
# Optional: only if different from the numeric prefix of OAuth:GOOGLE_DRIVE:ClientId
dotnet user-secrets set "OAuth:GOOGLE_DRIVE:PickerAppId" "YOUR_GOOGLE_CLOUD_PROJECT_NUMBER"
```

In Google Cloud Console enable the **Google Picker API** and **Google Drive API**,
and create an API key allowed for `http://localhost:5173`.
