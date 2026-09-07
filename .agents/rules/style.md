# C# style

The build has `TreatWarningsAsErrors`, `AnalysisMode=AllEnabledByDefault` and SonarAnalyzer. A
warning is a build failure, so style is not advisory here.

## Suppressions

Suppress in the **project file**, with a comment saying why, never in `Directory.Build.props` and
never with a bare `#pragma`. A suppression that applies to the whole solution hides the next real
finding.

## The house style

* File-scoped namespaces. One public type per file, named after the file.
* `sealed` by default. Primary constructors for dependencies.
* Explicit types for locals whose type is not obvious from the right-hand side; `var` is fine for
  `new`.
* XML documentation on every public member — `GenerateDocumentationFile` is on. Say what it does and
  why it exists, not what its name already says.
* `ConfigureAwait(false)` in library and framework code; not needed in endpoints or handlers.
* `CancellationToken` on every async method, passed down, never ignored.
* Never `DateTime.Now`. `TimeProvider` where it can be injected, `DateTimeOffset.UtcNow` otherwise.
* `Guid.CreateVersion7()` for identifiers, so they sort by creation time and index well.
* Invariant culture for anything that is not shown to a user (`string.Create(CultureInfo.InvariantCulture, $"...")`).

## Comments

Write the reason, not the mechanism. A comment that restates the code is noise; a comment that
records why an obvious-looking alternative was rejected saves the next person an afternoon. Most of
the comments in this repository exist because something bit somebody.

## Static state

Process-wide mutable state is a compare-exchange over an immutable snapshot, never a mutable
collection enumerated under concurrency. `PermissionConstants` is the pattern to copy.
