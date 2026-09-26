# CS2 Checker 2.0

Windows-программа для вспомогательной проверки компьютера при админ-проверке CS2.

## Важно
Программа не может гарантированно определить чит. Совпадение по имени или другой эвристике — только повод для дополнительной проверки.

## Сборка одного EXE

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

EXE появится в:

`bin\Release\net8.0-windows\win-x64\publish\CS2Checker.exe`

## GitHub

Файл `.github/workflows/release.yml` автоматически собирает Windows EXE при создании тега `v*`.
