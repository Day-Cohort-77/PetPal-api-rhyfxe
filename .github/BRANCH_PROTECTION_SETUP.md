# Branch Protection Rules Configuration

This is a quick reference for setting up GitHub branch protection rules to enforce automated testing.

## Quick Setup Checklist

### 1. Enable Branch Protection
- [ ] Go to Repository Settings → Branches
- [ ] Click "Add rule"
- [ ] Set branch name pattern: `main`
- [ ] Repeat for `develop` branch

### 2. Required Settings
```
☑️ Require a pull request before merging
    ☑️ Require approvals: 1
    ☑️ Dismiss stale PR approvals when new commits are pushed
    ☑️ Require review from code owners (if CODEOWNERS file exists) FUCK YOU

☑️ Require status checks to pass before merging
    ☑️ Require branches to be up to date before merging
    ☑️ Status checks (select these after first workflow run):
        - Run Tests / test
        - Code Quality and Coverage / code-quality

☑️ Require conversation resolution before merging

☑️ Require signed commits (optional but recommended) LOL NO XD

☑️ Require linear history (optional, prevents merge commits) LOL NO

☑️ Do not allow bypassing the above settings
    ☑️ Restrict pushes that create files larger than 100 MB
```

### 3. Advanced Settings (Optional)
``` 
☑️ Allow force pushes (for maintainers only)
☑️ Allow deletions (for maintainers only)
```

## Verification Steps

After setting up branch protection:

1. **Create a test branch**
   ```bash
   git checkout -b test-branch-protection
   ```

2. **Make a small change and push**
   ```bash
   echo "# Test" >> TEST.md
   git add TEST.md
   git commit -m "Test branch protection"
   git push origin test-branch-protection
   ```

3. **Create a pull request**
   - Go to GitHub and create PR from test branch to main
   - Verify that status checks appear and must pass
   - Verify that merge is blocked until checks pass

4. **Test failure scenario**
   - Add a failing test
   - Push to the test branch
   - Verify that PR shows failed checks and blocks merge

## Status Check Names

After your first workflow runs, these status checks will be available:

- **`Run Tests / test`** - Main testing workflow
- **`Code Quality and Coverage / code-quality`** - Coverage and quality checks

> **Note**: Status checks only appear in the list after they've run at least once. You may need to run your workflows first, then return to update the branch protection rules.

## Troubleshooting

### Status Checks Not Appearing
1. Ensure workflows have run at least once
2. Check that workflow names match exactly
3. Wait a few minutes and refresh the page

### Merge Still Allowed Despite Failed Tests
1. Verify "Do not allow bypassing" is checked
2. Ensure the correct status checks are selected
3. Check that branch name pattern is correct

### Tests Not Running on PR
1. Verify workflow triggers include `pull_request`
2. Check that target branch matches protection rules
3. Ensure repository has Actions enabled

---

**Quick Command Reference**:
```bash
# Check current branch protection status
gh api repos/:owner/:repo/branches/main/protection

# List all branches
gh api repos/:owner/:repo/branches

# View recent workflow runs
gh run list
```