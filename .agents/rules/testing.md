# Testing rules

* xunit.v3 on Microsoft.Testing.Platform, Shouldly for assertions, NSubstitute for fakes.
* `dotnet test --project <csproj>`. The platform opt-in lives in `global.json`.
* Name a test `Method_Should_Behaviour_When_Condition`, and group the class with
  `#region Happy Path`, `#region Exception Cases`, `#region Edge Cases`.
* Use `TestContext.Current.CancellationToken`, never `CancellationToken.None`, in a test body.
* A test that fails is telling you something. Before changing the test, satisfy yourself that the
  code is right — the currency check, the seeded permissions and the Identity indexes were all found
  by tests written against what the code *should* do.
* Do not stub the database to test a query filter. The integration suite runs the real composition
  root against a real Postgres for exactly this reason.
* Integration tests **skip** when no database is reachable rather than failing. A red suite that
  only means "no Docker here" trains people to ignore red suites.
* Architecture tests are a build gate, not documentation. Adding a rule there is cheaper than
  reviewing for it forever.
