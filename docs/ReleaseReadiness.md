# Release Readiness Checklist

This checklist is the release gate for the completed attribute-system roadmap.

## 1. Functional Parity Gate

- Runtime reflection and source-generated paths match for supported attributes.
- `docs/AttributeCompatibilityMatrix.md` reflects current support status.
- No unresolved parity issues remain in active sprint scope.

## 2. Diagnostics Gate

- Active source-generator diagnostics are documented in `docs/Attributes.md`.
- Negative coverage exists for all active IDs.
- Analyzer release files are aligned with active diagnostics.

## 3. Behavior Contract Gate

- `docs/Attributes.md` includes supported method signatures and precedence rules.
- `docs/CompatibilityNotes.md` documents effective behavior changes and compatibility constraints.
- `docs/ErrorHandling.md` includes all relevant runtime error codes.

## 4. Migration Gate

- `docs/MigrationRecipes.md` covers old-to-new patterns used in real projects.
- External-reference token and path-policy migration steps are documented.
- Factory mapping and helper visibility migration paths are documented.

## 5. Validation Gate

- Full Release test suite passes.
- Performance benchmark harness runs with advanced attribute scenarios.
- Aggregated benchmark reports are generated successfully.

## Rollout Notes

## Phase 1: Internal Verification

1. Run full tests in Release configuration.
2. Run benchmark harness and aggregate script.
3. Validate generated diagnostics on sample invalid cases.

## Phase 2: Consumer Communication

1. Publish compatibility notes and migration recipes with release notes.
2. Highlight strict factory validation and external-reference token behavior.
3. Share active diagnostics list and expected remediation paths.

## Phase 3: Adoption Monitoring

1. Track consumer reports for helper visibility and factory mapping errors.
2. Track performance deltas for advanced preprocessor/external-ref scenarios.
3. Update migration recipes when recurring integration patterns appear.
