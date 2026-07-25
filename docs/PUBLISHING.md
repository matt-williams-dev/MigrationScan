# Publishing a release

Everything ships from one tag. Push `v0.1.0` and the release workflow builds the executables,
creates the GitHub release, and pushes the package to nuget.org.

```console
# 1. Bump the version and write the changelog entry
#    Directory.Build.props → <Version>0.1.0</Version>
#    CHANGELOG.md          → move Unreleased items under the new heading
git commit -am "Release 0.1.0"

# 2. Tag it and push
git tag v0.1.0
git push origin main --follow-tags
```

The tag must match `<Version>` exactly, minus the leading `v`. The workflow checks this and fails
before publishing anything — the GitHub release and nuget.org disagreeing about what `v0.1.0`
contains is not fixable afterwards, because **nuget.org is append-only**. A published version can
be unlisted, never replaced or reused.

## One-time setup

### Trusted Publishing

There is **no API key to create, store or rotate**. The publish job proves its identity to
nuget.org with a short-lived GitHub OIDC token and gets back a key valid for one hour. Nothing
long-lived exists to leak, and a stolen workflow log is worth nothing an hour later.

**On nuget.org:** sign in, click your username → **Trusted Publishing**, and add a policy:

| Field | Value |
| --- | --- |
| Repository Owner | `matt-williams-dev` |
| Repository | `MigrationScan` |
| Workflow File | `release.yml` |
| Environment | `nuget` |

Two things that are easy to get wrong:

- **Workflow File is the file name only.** `release.yml`, *not* `.github/workflows/release.yml`.
- **Environment must match** the `environment:` on the publish job. It is optional in general,
  but this workflow sets `environment: nuget`, so leaving the field blank will not match.

**In this repository:** add one secret, **Settings → Secrets and variables → Actions**, named
`NUGET_USER`, holding your **nuget.org profile name** — not your email address. It is not really
a secret (it appears on every package page you own), but nuget.org recommends it as one and the
workflow fails early with a clear message if it is missing.

> **A policy covers packages that do not exist yet.** Policies are scoped to an *owner*, not to a
> package, so the first-publish chicken-and-egg an API key has — a glob-scoped key cannot match an
> ID nothing has been published under — simply does not arise. This is why Trusted Publishing is
> the better fit here, not just the more fashionable one.

> **If you do not see the Trusted Publishing option**, nuget.org is still rolling it out
> gradually and your account may not have it. Fall back to an API key: create one at **API Keys**
> → **Create** (push-only, unscoped for the first publish since the package does not exist yet),
> store it as `NUGET_API_KEY`, and in the `nuget` job replace the login step with
> `--api-key ${{ secrets.NUGET_API_KEY }}` and drop the `id-token: write` permission.

### Reserve the package ID (recommended, after the first publish)

`MigrationScan.Tool` and `MigrationScan` were both free as of 2026-07-25. An ID prefix reservation
stops anyone else publishing `MigrationScan.Anything` and gives the package the verified-owner
tick on nuget.org, which matters for a tool asking to be trusted with somebody's source. Request
it at <https://www.nuget.org/policies/PackageIdPrefixReservation>. It is a manual review that
looks at packages you already own, so it follows the first publish rather than preceding it, and
it takes days — start it as soon as `0.1.0` is live.

### The `nuget` environment

The push job runs in a GitHub environment named `nuget`, which does double duty here. Creating it
under **Settings → Environments** and adding yourself as a required reviewer makes every publish
pause for a manual approval — worth it for an append-only destination. It is also named in the
trusted publishing policy, so a workflow running outside that environment cannot obtain a key
even if it is otherwise identical.

Create it, or the policy's Environment field will not match and the token exchange will be
refused.

## What the workflow does

`.github/workflows/release.yml`, on a `v*` tag:

| Job | |
| --- | --- |
| `test` | Full suite on Ubuntu. Everything else depends on it, so a red build publishes nothing. |
| `verify-version` | Tag matches `<Version>`. Fails fast, before any artifact exists. |
| `publish` | Self-contained single-file executables for win-x64, win-arm64, linux-x64, osx-arm64, each on its own OS, archived with a `.sha256`. |
| `release` | Creates the GitHub release and attaches the archives, checksums, and the `.nupkg`. |
| `nuget` | Exchanges an OIDC token for a one-hour key and pushes `.nupkg` + `.snupkg` to nuget.org. Last, and only after the release exists. |

The key is requested *after* packing and immediately before the push. It is valid for an hour and
each token buys exactly one key, so asking early risks a slow build expiring it.

`id-token: write` is granted on the `nuget` job alone, never at workflow level. That permission
lets anything in the job mint an identity token, so only the job that actually publishes should
hold it. Declaring `permissions:` on a job replaces the workflow default outright, which is why
that block also restates `contents: read` for the checkout.

`workflow_dispatch` runs everything except `verify-version`, `release` and `nuget` — so the build
and packaging path can be exercised without minting anything.

## Checking a package before it goes out

```console
dotnet pack src/MigrationScan.Tool -c Release -o /tmp/pack -p:ContinuousIntegrationBuild=true

# What is actually in it
unzip -l /tmp/pack/MigrationScan.Tool.*.nupkg

# What nuget.org will render
unzip -p /tmp/pack/MigrationScan.Tool.*.nupkg MigrationScan.Tool.nuspec

# Prove it installs and runs, without touching your real tool set
dotnet tool install MigrationScan.Tool --add-source /tmp/pack --tool-path /tmp/toolroot
/tmp/toolroot/migrationscan --help
rm -rf /tmp/toolroot
```

That last step is the one worth not skipping. `dotnet pack` succeeding says nothing about whether
the tool starts — the manifest, the entry point and the assembly name all have to line up, and
they are easy to break without any build error.

## Versioning

`<Version>` in `Directory.Build.props` is the single knob; every assembly and the package take it.
It is pinned rather than derived from the build so that a report's `scan.toolVersion` is stable
and two people on the same release produce byte-identical output.

Semantic versioning applies to the **tool**. The **report schema** is versioned separately (see
[`docs/schema`](schema/README.md)) — a schema minor is always additive, and a consumer written
against an older minor keeps working. The two numbers move independently and should not be
conflated.

For a prerelease, use a suffix: `<Version>0.2.0-preview.1</Version>` tagged `v0.2.0-preview.1`.
nuget.org hides prereleases from default search, and `dotnet tool install` needs `--prerelease`
to see them.

## If a release goes wrong

- **Before the `nuget` job ran:** delete the tag and the GitHub release, fix, retag. Nothing is
  permanent yet.
- **After it ran:** the version on nuget.org is permanent. Unlist it
  (**Manage package → Listing**) so it stops appearing in search and in `dotnet tool install`
  without a version, then publish a fixed higher version. Unlisting does not break anyone who
  already depends on that exact version, which is the point.
- **A failed push, package already there:** the push uses `--skip-duplicate`, so re-running the
  job is safe. Re-running also mints a fresh key, so an expired one is not a reason to retag.
- **The token exchange is refused:** the policy and the run have to agree on all four of owner,
  repository, workflow file name and environment. Check the workflow file field is `release.yml`
  and not a path, and that the `nuget` environment exists. If the policy shows as *pending*, it
  is in the 7-day activation window nuget.org applies to some repositories — a successful publish
  inside that window makes it permanent, and the window can be restarted at any time.
- **`NUGET_USER` missing:** the job fails before the exchange with a message naming the secret,
  rather than surfacing as an opaque authentication error.
