# Test fixtures

Small but realistic legacy solutions used by rule and report tests. Planned set
(spec §12): a WebForms app, a WCF service, a WinForms app with COM interop, a class
library with `packages.config`, and one clean modern solution that must produce zero
findings (the false-positive guard).

Each rule gets a positive fixture (produces the finding) and a negative fixture
(produces none). Fixtures are added alongside the rules they exercise, starting in
Phase 1.
