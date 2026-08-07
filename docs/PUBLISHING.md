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

The tag must match `<Version>` exactly, minus the leading `v`. The workflow checks this and
fails before publishing anything. You cannot fix a GitHub release and nuget.org disagreeing about
what `v0.1.0` contains, because **nuget.org is append-only**. You can unlist a published version.
You can never replace or reuse one.

## One-time setup

### Trusted Publishing

You create, store and rotate **no API key**. The publish job proves its identity to nuget.org
with a short-lived GitHub OIDC token and gets back a key valid for one hour. Nothing long-lived
exists to leak, and a stolen workflow log is worthless an hour later.

**On nuget.org:** sign in, click your username → **Trusted Publishing**, and add a policy:

| Field | Value |
| --- | --- |
| Repository Owner | `matt-williams-dev` |
| Repository | `MigrationScan` |
| Workflow File | `release.yml` |
| Environment | `nuget` |

Two things that are easy to get wrong:

- **Workflow File takes the file name only.** `release.yml`, not `.github/workflows/release.yml`.
- **Environment must match** the `environment:` on the publish job. The field is optional in
  general, but this workflow sets `environment: nuget`, so a blank field will not match.

**In this repository:** add one secret, **Settings → Secrets and variables → Actions**, named
`NUGET_USER`, holding your **nuget.org profile name** rather than your email address. It is
barely a secret, since it appears on every package page you own, but nuget.org recommends storing
it as one and the workflow fails early with a clear message when it is missing.

> **A policy covers packages that do not exist yet.** Policies attach to an *owner*, not to a
> package. An API key has a chicken-and-egg problem here: a glob-scoped key matches no ID until
> something has been published under it. A policy has no such problem, which is the practical
> reason to prefer Trusted Publishing rather than the fashionable one.

> **If you do not see the Trusted Publishing option**, nuget.org is still rolling it out
> gradually and your account may not have it. Fall back to an API key: create one at **API Keys**
> → **Create** (push-only, unscoped for the first publish since the package does not exist yet),
> store it as `NUGET_API_KEY`, and in the `nuget` job replace the login step with
> `--api-key ${{ secrets.NUGET_API_KEY }}` and drop `id-token: write` from that job's
> `permissions:` block. Leave the workflow-level one alone: the `publish` job signs with it.

### Reserve the package ID (recommended, after the first publish)

`MigrationScan.Tool` and `MigrationScan` were both free as of 2026-07-25. An ID prefix reservation
stops anyone else publishing `MigrationScan.Anything` and gives the package the verified-owner
tick on nuget.org, which matters for a tool asking to be trusted with somebody's source. Request
it at <https://www.nuget.org/policies/PackageIdPrefixReservation>. A human reviews it, and they
look at packages you already own, so it follows your first publish rather than preceding it. It
takes days, so start it as soon as `0.1.0` is live.

### The `nuget` environment

The push job runs in a GitHub environment named `nuget`, which earns its keep twice. Create it
under **Settings → Environments**, add yourself as a required reviewer, and every publish pauses
for your approval. That is worth having on an append-only destination. The policy also names the
environment, so a workflow running outside it cannot get a key even when everything else matches.

Create it, or the policy's Environment field will not match and the token exchange will be
refused.

## Code signing

The `publish` job signs the Windows and macOS builds. Both sets of credentials sit in a GitHub
environment named `release`, which the job declares, so create it under **Settings →
Environments** before the next tag. The Azure federated credential names that environment in its
subject, so a job running without it gets a token Azure refuses.

**Windows** goes through Azure Artifact Signing, formerly Trusted Signing. The rename reached the
GitHub Action too, so the workflow calls `azure/artifact-signing-action`. Authentication is an OIDC
federated credential, so no client secret sits in the repository waiting to leak.

| Secret | |
| --- | --- |
| `AZURE_CLIENT_ID` | The app registration the federated credential belongs to. |
| `AZURE_TENANT_ID` | |
| `AZURE_SUBSCRIPTION_ID` | |

| Variable | |
| --- | --- |
| `SIGNING_ENDPOINT` | Regional endpoint for the account, shaped like `https://eus.codesigning.azure.net`. No trailing slash, the service refuses the request with one. |
| `SIGNING_ACCOUNT` | Signing account name. |
| `SIGNING_PROFILE` | Certificate profile name. |

Lose the Azure secrets and the Windows signing steps skip, leaving unsigned binaries and a green
workflow. macOS gets no such escape hatch: a missing Apple credential fails the job, because a
download that trips Gatekeeper is worse than no macOS download at all.

**macOS** needs a Developer ID certificate pair for signing and an App Store Connect key for
notarization.

| Secret | |
| --- | --- |
| `APPLE_CERTS_P12_BASE64` | One base64 `.p12` holding both the Developer ID Application and Developer ID Installer identities. |
| `APPLE_CERTS_P12_PASSWORD` | |
| `APPLE_API_KEY_P8_BASE64` | The App Store Connect `.p8`, base64. |
| `APPLE_API_KEY_ID` | |
| `APPLE_API_ISSUER_ID` | |

| Variable | |
| --- | --- |
| `APPLE_TEAM_ID` | Picks the right pair out of the keychain when more than one identity matches. |

The job imports the `.p12` into a keychain it creates with a password generated at run time and
deletes in an `always()` step. It signs the executable under the hardened runtime with an
entitlements plist, since a single-file build extracts and loads its own native libraries. It then
builds a `.pkg`, signs that with the installer identity, sends it to `notarytool submit --wait`
and staples the ticket.

Two details there are load-bearing. `--timestamp` keeps every copy already downloaded verifying
after the certificate expires on 2027-02-01. And the ticket staples to the `.pkg` rather than to
the executable, which is the only way Gatekeeper clears the tool on a machine with no route out,
which is the machine this tool exists for.

## What the workflow does

`.github/workflows/release.yml`, on a `v*` tag:

| Job | |
| --- | --- |
| `test` | Full suite on Ubuntu. Everything else depends on it, so a red build publishes nothing. |
| `verify-version` | Tag matches `<Version>`. Fails fast, before any artifact exists. |
| `publish` | Self-contained single-file executables for win-x64, win-arm64, linux-x64, osx-x64, osx-arm64. Signs the Windows and macOS builds, notarizes and staples the macOS `.pkg`, and writes a `.sha256` for every asset. |
| `release` | Creates the GitHub release and attaches the archives, checksums, and the `.nupkg`. |
| `nuget` | Exchanges an OIDC token for a one-hour key and pushes `.nupkg` + `.snupkg` to nuget.org. Last, and only after the release exists. |

The job asks for the key after packing and immediately before the push. A key lives an hour and
each token buys exactly one, so asking early risks a slow build expiring it.

Two jobs trade an OIDC token for a short-lived credential: `publish`, for Azure Artifact Signing,
and `nuget`, for the one-hour push key. `id-token: write` therefore sits at workflow level. The
`nuget` job restates it anyway, because declaring `permissions:` on a job replaces the workflow
defaults outright rather than adding to them, and that block drops `contents` back to `read`.

`workflow_dispatch` runs everything except `verify-version`, `release` and `nuget`, so you can
exercise the build and packaging path without minting anything.

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

Do not skip that last step. A successful `dotnet pack` says nothing about whether the tool
starts: the manifest, the entry point and the assembly name all have to line up, and you can break
any of them without seeing a build error.

## Versioning

`<Version>` in `Directory.Build.props` is the single knob; every assembly and the package take it.
It is pinned rather than derived from the build so that a report's `scan.toolVersion` is stable
and two people on the same release produce byte-identical output.

Semantic versioning applies to the **tool**. The **report schema** carries its own version (see
[`docs/schema`](schema/README.md)). A schema minor is additive, and a consumer written against an
older minor keeps working. The two numbers move independently, so do not conflate them.

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
  is in the 7-day activation window nuget.org applies to some repositories. A successful publish
  inside that window makes it permanent, and you can restart the window at any time.
- **`NUGET_USER` missing:** the job fails before the exchange with a message naming the secret,
  rather than surfacing as an opaque authentication error.
