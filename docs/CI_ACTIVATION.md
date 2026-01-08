# GitHub Actions CI/CD Activation Guide

This document explains how to enable the GitHub Actions workflows for BPS Patch.

**Status**: All workflows are **DISABLED BY DEFAULT** to prevent unexpected costs.

---

## Why CI/CD is Disabled by Default

GitHub Actions provides free minutes for public repositories, but:

1. **Private repositories have limited free minutes** (2,000/month for free tier)
2. **Costs can accumulate quickly** with frequent commits
3. **Matrix builds multiply usage** (3 OS × builds = 3× minutes per run)
4. **You may not need it** for personal/experimental projects

The workflows are configured to run **only on manual trigger** until you explicitly enable them.

---

## Available Workflows

| Workflow | File | Purpose | Estimated Time |
|----------|------|---------|----------------|
| CI Build and Test | `ci.yml` | Build & test on 3 platforms | ~5-10 min |
| Code Coverage | `coverage.yml` | Generate coverage reports | ~3-5 min |
| Release | `release.yml` | Create releases & NuGet packages | ~5 min |

---

## Running Workflows Manually (Recommended for Testing)

You can run any workflow manually without enabling automatic triggers:

1. Go to your repository on GitHub
2. Click **Actions** tab
3. Select the workflow (e.g., "CI Build and Test")
4. Click **Run workflow** button
5. Select branch and options
6. Click **Run workflow**

This lets you test CI without any ongoing costs.

---

## Enabling Automatic CI

### Option 1: Enable CI on Push/PR (Recommended for Active Development)

Edit `.github/workflows/ci.yml` and uncomment the trigger lines:

```yaml
on:
  workflow_dispatch:  # Keep manual trigger
  
  # UNCOMMENT THESE LINES:
  push:
	branches: [ master, main, develop ]
	paths-ignore:
	  - '**.md'
	  - 'docs/**'
  pull_request:
	branches: [ master, main ]
	paths-ignore:
	  - '**.md'
	  - 'docs/**'
```

**Cost estimate**: ~5-10 minutes per push × 3 platforms = 15-30 minutes per commit

### Option 2: Enable CI on PR Only (Lower Cost)

Only run CI when pull requests are created:

```yaml
on:
  workflow_dispatch:
  pull_request:
	branches: [ master, main ]
```

**Cost estimate**: ~15-30 minutes per PR (not every commit)

### Option 3: Single Platform Only (Lowest Cost)

Modify the matrix to test on one platform only:

```yaml
strategy:
  matrix:
	os: [ubuntu-latest]  # Remove windows-latest, macos-latest
```

**Cost estimate**: ~5-10 minutes per run (Linux only)

---

## Enabling Automatic Coverage

Edit `.github/workflows/coverage.yml`:

```yaml
on:
  workflow_dispatch:
  
  # UNCOMMENT:
  push:
	branches: [ master, main ]
  pull_request:
	branches: [ master, main ]
```

### Setting Up Codecov (Optional)

1. Go to [codecov.io](https://codecov.io) and sign in with GitHub
2. Add your repository
3. Copy the upload token
4. Add it as a repository secret: Settings → Secrets → `CODECOV_TOKEN`
5. Enable the upload in workflow inputs

---

## Enabling Automatic Releases

Edit `.github/workflows/release.yml`:

```yaml
on:
  workflow_dispatch:
  
  # UNCOMMENT for tag-based releases:
  push:
	tags:
	  - 'v*'
```

Then create releases by pushing tags:

```bash
git tag v1.0.0
git push origin v1.0.0
```

### Setting Up NuGet Publishing

1. Go to [nuget.org](https://nuget.org) and sign in
2. Go to API Keys → Create
3. Create a key with push permissions for your package
4. Add it as a repository secret: Settings → Secrets → `NUGET_API_KEY`

---

## Cost Management Tips

### Monitor Usage

1. Go to Settings → Billing → Actions
2. Review minutes used this month
3. Set up spending limits if needed

### Reduce Costs

1. **Skip CI for documentation changes** (already configured with `paths-ignore`)
2. **Use `[skip ci]` in commit messages** to skip CI:
   ```
   git commit -m "docs: update readme [skip ci]"
   ```
3. **Limit matrix builds** to essential platforms
4. **Use caching** (already configured in workflows)
5. **Run expensive jobs (benchmarks) only on manual trigger**

### Self-Hosted Runners (Advanced)

For heavy usage, consider [self-hosted runners](https://docs.github.com/en/actions/hosting-your-own-runners):

```yaml
runs-on: self-hosted  # Instead of ubuntu-latest
```

---

## Quick Reference

| Action | How |
|--------|-----|
| Run CI manually | Actions tab → CI Build and Test → Run workflow |
| Enable auto CI | Uncomment `push`/`pull_request` in `ci.yml` |
| Disable auto CI | Comment out `push`/`pull_request` triggers |
| Skip CI for one commit | Add `[skip ci]` to commit message |
| Check costs | Settings → Billing → Actions |

---

## Why You Should Enable CI (Eventually)

Once your project stabilizes, enabling CI provides:

1. **Confidence**: Every change is tested automatically
2. **Cross-platform verification**: Catch OS-specific bugs early
3. **Documentation**: Build status badges show project health
4. **Collaboration**: PRs are tested before merge
5. **Release automation**: Consistent, repeatable releases

**Recommendation**: Start with manual triggers, enable automatic CI when you're ready for regular development.

---

## Adding Status Badges

Once CI is enabled, add badges to your README:

```markdown
![Build Status](https://github.com/TheAnsarya/bps-patch/actions/workflows/ci.yml/badge.svg)
![Coverage](https://github.com/TheAnsarya/bps-patch/actions/workflows/coverage.yml/badge.svg)
```
