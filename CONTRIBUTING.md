# Contributing to FlbxStudio

Thanks for contributing.

## Getting started

1. Fork the repository.
2. Create a feature branch from `main`.
3. Make focused changes with clear commit messages.
4. Run a local build before opening a pull request.

```powershell
dotnet build FlbxStudio/FlbxStudio.csproj -c Debug
```

## Development guidelines

- Target framework: `.NET 10` (`net10.0-windows`).
- Keep pull requests small and scoped.
- Follow existing code style and naming conventions.
- Add XML documentation comments for public API changes.
- Update `README.md` / `CHANGELOG.md` when behavior changes.

## Pull request checklist

- [ ] Build succeeds locally.
- [ ] Changes are tested manually for affected workflows.
- [ ] Public-facing behavior changes are documented.
- [ ] No unrelated refactors included.

## Reporting issues

Use the provided issue templates:

- **Bug report** for reproducible defects
- **Feature request** for proposed improvements

Include reproduction steps, expected behavior, actual behavior, and environment details.
