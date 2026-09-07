# Architecture

A longer form of what `AGENTS.md` states operationally. Read that first if you are about to change
something; read this if you want to know why it is shaped this way.

## Why a modular monolith

Microservices buy independent deployment at the cost of a network between every two pieces of your
domain. A practice-management system has a small number of tightly-related concepts — a patient, an
appointment, a procedure, an invoice — and a chart entry that cannot see its own procedures is
useless. So the default is one process, with boundaries enforced at compile time rather than by a
network.

What makes it *modular* rather than merely a monolith:

* Each module is a pair of projects, and a module can only reference another's **Contracts**. The
  architecture tests fail the build otherwise, so the boundary is not a convention that erodes.
* Modules communicate by event where they can, and by a narrow contract service where they must.
* Each module owns a schema. No module queries another's tables.

The result is that extracting a module is a deployment decision, not a rewrite. Notifications is the
proof: it runs in its own process, with its own database and its own migrator, and its code is
identical to when it ran in the API.

## Why vertical slices

Layered architectures spread one behaviour across four projects, so a one-line change to booking
touches a controller, a service, a repository and a DTO mapper in four directories. A slice puts the
endpoint, the command, the handler and the validator in one folder. The cost is some repetition
between slices; the benefit is that a feature is deletable, and that nobody has to invent a service
class to hold code that is used exactly once.

## Multi-tenancy

Shared schema, tenant column, Finbuckle for resolution and filtering.

Tenant resolution runs **before authentication**, because the tenant decides which user store the
credentials are checked against. That is why it is header-driven: at the point resolution happens
there is no validated token to read a claim from.

Three things follow, and all three have bitten:

* `EnforceMultiTenant()` refuses a write whose tenant does not match the context's. Auditing had to
  learn that the trail row belongs to the tenant that *made* the change, not the tenant named by the
  row that changed.
* Every unique index must include the tenant column. ASP.NET Identity's own indexes do not, so they
  are removed from the model and replaced.
* A background handler has no ambient tenant. It restores one from the event — in two steps, because
  an AsyncLocal set inside an awaited helper does not flow back to the caller.

## Eventing

Two tiers, deliberately not one.

**Domain events** are in-process, in-transaction, inside a module. They exist so an aggregate can
announce something without the handler that changed it knowing every consequence. Every one must
have a handler; the source generator enforces it, and that is the right rule — an event nobody
handles is a lie about the design.

**Integration events** cross modules and processes, always through the outbox. The outbox row is
written in the same transaction as the change it describes, so the fact and its announcement cannot
disagree. Delivery is at-least-once, so the inbox deduplicates on `(EventId, HandlerName)`, and the
claim commits with the handler's own save — an inbox that commits first turns a transient failure
into a lost event.

## What the tests are for

The architecture suite is a build gate: boundaries, naming, tenant markers, authorization metadata.
Adding a rule there is cheaper than reviewing for it forever.

The integration suite runs the real composition root against a real Postgres, substituting nothing.
That is not thoroughness for its own sake — a suite that stubs the database cannot see a broken
query filter, a globally-unique index, or a permission that was never granted to anybody. All three
were found that way.
