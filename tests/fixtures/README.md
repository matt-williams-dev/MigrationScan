# Test fixtures

Small but realistic legacy solutions used by rule and report tests. Planned set
(spec §12): a WebForms app, a WCF service, a WinForms app with COM interop, a class
library with `packages.config`, and one clean modern solution that must produce zero
findings (the false-positive guard).

Each rule gets a positive fixture (produces the finding) and a negative fixture
(produces none). Fixtures are added alongside the rules they exercise, starting in
Phase 1.

## Current fixtures

- **LegacyWebForms** — non-SDK WebForms app. Triggers MIG1001 (non-SDK), MIG1002
  (`packages.config`), MIG1005 (Telerik GAC reference), MIG2001 (`Microsoft.AspNet.Mvc`),
  MIG3001 (WebForms), and MIG5001 (`ConfigurationManager.AppSettings`).
- **LegacyLibrary** — non-SDK class library referencing `System.Web` with no WebForms
  markup. Triggers MIG1001 and MIG3002 (System.Web outside WebForms).
- **LegacySyntax** — SDK-style project whose `.cs` files exercise each Tier 2 syntax rule
  (MIG3004/3005/3010, MIG4001/4002/4004/4008, MIG6001/6004, MIG7001, MIG8002/8003). SDK-style
  on purpose, so only the syntax findings appear.
- **NestedProjects** — a project wrapping a nested project and a `.hidden` folder; guards
  against absorbing files that belong to neither (must yield zero findings).
- **GacHintPath** — empty vs. real `<HintPath>` edge case for MIG1005.
- **StaleReference** — solution referencing a missing project (skipped with a warning).
- **LegacyVbApp** — non-SDK **VB.NET** console app (`.vbproj`) with `System.Web`, a
  `packages.config`, and a `Module1.vb` that uses ConfigurationManager, the Registry,
  BinaryFormatter, and encodings. Exercises `.vbproj` discovery, the project-level rules
  (MIG1001/1002/2001/3002), and the VB **syntax** rules (MIG5001/4002/6001/8002/8003).
- **ModernClean** — clean SDK-style app. Must produce **zero** findings; this is the
  false-positive guard (spec §12).
