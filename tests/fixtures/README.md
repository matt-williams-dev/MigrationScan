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
- **ModernClean** — clean SDK-style app. Must produce **zero** findings; this is the
  false-positive guard (spec §12).
