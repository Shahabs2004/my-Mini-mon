# Protecting the main branch

The repository currently has no active repository rulesets visible through the GitHub API. The connected GitHub integration can commit repository files, but it does not expose the administrative write operation required to enable branch protection. A repository administrator must complete the one-time setup below in GitHub.

## Recommended settings

1. Open **Settings → Rules → Rulesets** (or **Settings → Branches → Add branch protection rule**).
2. Target the `main` branch.
3. Enable **Require a pull request before merging**. For a solo-maintained project, one approval is optional; the important part is requiring the checks below.
4. Enable **Require status checks to pass before merging**.
5. Add the check emitted by this workflow:
   - `Restore, build and test (.NET 8 / Windows)`
6. Enable **Block force pushes** and **Restrict deletions**.
7. Save the rule and verify that a pull request cannot merge while the required check is failing or pending.

## Workflow behavior

The workflow at `.github/workflows/build.yml` runs on pushes to `main`, pull requests targeting `main`, and manual dispatch. It restores the solution, compiles the full solution in Release mode, runs automated tests, and uploads build/test artifacts.

The build artifact is uploaded whenever compilation succeeds, even if a later test step fails. Test results are uploaded with `if: always()` so a failed test run remains inspectable.

## Important

A status check can only be selected as a required check after GitHub has observed it at least once. If the check is not listed yet, push a commit or use **Actions → Windows Build and Test → Run workflow**, wait for the first run to finish, then return to the ruleset configuration.

Do not mark branch protection as enabled until the rule is saved and verified in GitHub.
