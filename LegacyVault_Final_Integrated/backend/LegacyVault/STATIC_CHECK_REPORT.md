# Static Check Report

Generated from the merged source package.

- C# source files scanned: 66
- Brace-balance issues: 0
- Duplicate class names inside the same namespace: 0
- Existing IdentityAccess files compared byte-for-byte with uploaded project: 23 checked, all unchanged
- `.vs`, `bin`, `obj`: excluded from final package
- `Program.cs`: intentionally merged for DI only
- `LegacyVaultDbContext.cs`: intentionally merged for Asset/Will mappings

## Important limitation

The execution environment used to prepare this package does not have the .NET SDK, so `dotnet build` could not be run here.

The first local validation step is therefore:

```powershell
dotnet restore
dotnet build
```

or **Build Solution** in Visual Studio.

After that, run the requests in `LegacyVault.http`.
