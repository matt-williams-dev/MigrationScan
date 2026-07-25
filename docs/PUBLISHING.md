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

### The nuget.org API key

Required before the first release. Without it the push step logs a warning and skips; the GitHub
release still publishes, so a missed key costs a re-run rather than a broken release.

1. Sign in at <https://www.nuget.org/> and go to **API Keys** → **Create**.
2. Scope it as tightly as it will go:
   - **Key name:** `MigrationScan release workflow`
   - **Expires:** 365 days (the maximum). Put a reminder in the calendar — an expired key fails a
     release at the last step.
   - **Scopes:** *Push* only. Not *Unlist*.
   - **Glob pattern:** `MigrationScan.*` — so a leaked key cannot push to anything else you own.
3. Copy the key. It is shown once.
4. Add it to the repository: **Settings → Secrets and variables → Actions → New repository
   secret**, named `NUGET_API_KEY`.

> The first push of a brand-new package ID cannot use a glob-scoped key, because the package does
> not exist yet and the glob matches nothing. Either create the first key with **Push new packages
> and package versions** unscoped, publish `0.1.0`, then replace it with the globbed key — or
> reserve the ID first (below).

### Reserve the package ID (optional, recommended)

`MigrationScan.Tool` and `MigrationScan` were both free as of 2026-07-25. An ID prefix reservation
stops anyone else publishing `MigrationScan.Anything` and gives the package the verified-owner
tick on nuget.org, which matters for a tool asking to be trusted with somebody's source. Request
it at <https://www.nuget.org/policies/PackageIdPrefixReservation> — it is a manual review and
takes days, so start it before you need it.

### The `nuget` environment

The push job runs in a GitHub environment named `nuget`. Creating it under **Settings →
Environments** and adding yourself as a required reviewer means every publish pauses for a manual
approval. Worth it for an append-only destination. If the environment does not exist the job runs
without the gate.

## What the workflow does

`.github/workflows/release.yml`, on a `v*` tag:

| Job | |
| --- | --- |
| `test` | Full suite on Ubuntu. Everything else depends on it, so a red build publishes nothing. |
| `verify-version` | Tag matches `<Version>`. Fails fast, before any artifact exists. |
| `publish` | Self-contained single-file executables for win-x64, win-arm64, linux-x64, osx-arm64, each on its own OS, archived with a `.sha256`. |
| `release` | Creates the GitHub release and attaches the archives, checksums, and the `.nupkg`. |
| `nuget` | Packs again and pushes `.nupkg` + `.snupkg` to nuget.org. Last, and only after the release exists. |

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
  job is safe.
