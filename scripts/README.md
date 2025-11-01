# Development Scripts

Local validation and development task automation.

## Quick Start

```powershell
# Validate before pushing (catches CI failures locally)
.\run.ps1 validate

# Fast validation (skip publish test)
.\run.ps1 validate-fast

# Run Aspire development mode
.\run.ps1 aspire
```

## Available Commands

Run `.\run.ps1 help` for full list of tasks.

### Pre-Push Validation

The `validate-build.ps1` script simulates the GitHub Actions CI pipeline locally:

- ✅ Restore dependencies
- ✅ Build in Release mode (like CI)
- ✅ Run all tests with coverage
- ✅ Check for vulnerable packages
- ✅ Validate assembly version strings
- ✅ Test publish with version parameters

**Usage:**

```powershell
# Full validation (recommended before PR)
.\scripts\validate-build.ps1

# Fast mode (skip publish test)
.\scripts\validate-build.ps1 -Fast

# Skip tests (build + security only)
.\scripts\validate-build.ps1 -SkipTests

# Skip security scan
.\scripts\validate-build.ps1 -SkipSecurity
```

### Git Pre-Push Hook

The `.git/hooks/pre-push` hook automatically runs validation before every push.

**Bypass in emergencies:**

```powershell
# Temporarily skip validation
$env:SKIP_VALIDATION = "1"
git push

# Or inline
$env:SKIP_VALIDATION = "1"; git push
```

### Task Runner

The `run.ps1` script provides quick access to common tasks:

```powershell
# Build
.\run.ps1 build

# Test
.\run.ps1 test

# Security scan
.\run.ps1 security

# Clean
.\run.ps1 clean
```

## Act - GitHub Actions Locally

For exact CI simulation, use [Act](https://github.com/nektos/act):

```powershell
# Install
winget install nektos.act

# List workflows
act -l

# Run specific job (if you have Docker/Podman configured)
act -j code-quality
```

**Note**: Act requires Docker or Podman. On Windows with Podman WSL, configuration can be complex. **Recommendation**: Use `.\run.ps1 validate` instead - it's faster, native, and more accurate for .NET projects.

## CI/CD Pipeline Simulation

The validation script mimics these CI steps:

1. **Code Quality** - Build with analyzers
2. **Unit Tests** - All test projects with coverage
3. **Security** - Vulnerable package scan
4. **Service Build** - Publish with versioning
5. **Aspire Build** - AppHost publish

Run locally to catch issues before CI:

```powershell
# Before creating PR
.\run.ps1 validate

# Quick check before push
.\run.ps1 validate-fast
```

## Performance

- **Full validation**: ~30-60 seconds
- **Fast validation**: ~15-30 seconds (skip publish test)
- **Pre-push hook**: ~20-40 seconds (auto-runs on push)

## Troubleshooting

### Pre-push hook not running

Ensure the hook is executable:

```powershell
# Should already be set, but if needed:
git config core.hooksPath .git/hooks
```

### Validation too slow

Use fast mode or skip tests during rapid iteration:

```powershell
.\run.ps1 validate-fast
# or
.\scripts\validate-build.ps1 -SkipTests
```

### CI passes but local fails (or vice versa)

Ensure you're using the same .NET version as CI:

```powershell
dotnet --version  # Should be 9.0.x
```

## Best Practices

1. **Before every push**: Run `.\run.ps1 validate-fast`
2. **Before creating PR**: Run `.\run.ps1 validate`
3. **During development**: Use `.\run.ps1 test` for quick feedback
4. **After dependency changes**: Run `.\run.ps1 security`

## Integration with IDEs

### VS Code

Add to `.vscode/tasks.json`:

```json
{
  "label": "Validate Build",
  "type": "shell",
  "command": "${workspaceFolder}/run.ps1 validate",
  "problemMatcher": []
}
```

### Visual Studio

Add as External Tool in Tools > External Tools.

## Future Enhancements

- [ ] Parallel test execution
- [ ] Coverage threshold validation
- [ ] Code formatting checks (dotnet format)
- [ ] Markdown linting for docs
- [ ] Installer build validation
