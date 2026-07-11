# Commands

## Inspect
```powershell
rg --files Assets/BombRunner
rg -n --glob '*.cs' -S "class |interface |enum " Assets/BombRunner/Scripts
git status --short
```

## Build
```powershell
dotnet build Assembly-CSharp.csproj -nologo -v minimal
```

## Notes
- Unity-generated files and third-party art assets can create large search output. Prefer `--glob '*.cs'` for code searches.
- Git may warn about the user-level ignore file permission; that warning is not a project compile failure.
