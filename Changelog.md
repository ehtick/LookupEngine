# 1.1.0

## Global changes

- Migrated from `Nuke` to `ModularPipelines` build system.
- Support for .NET 10.
- Removed legacy `.sln` file in favor of `.slnx`.

## Engine

- Added support for serialization of decomposition results.
- Added exception handling for fields evaluation.
- Improved enumerator handling with proper disposal.
- Improved `DeclaringTypeFullName` formatting for types in global namespace.
- Replaced directives with `#if NET` for better cross-framework support.

## Performance

- Added new performance benchmarks for object decomposition.
- Refactored existing benchmarks for better organization and naming consistency.
- Optimized memory allocations in core engine paths.

## Testing

- Added ~100 new tests for the engine.

# 1.0.0

Initial release. Enjoy!