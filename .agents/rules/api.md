# API rules

## Endpoints

* Minimal APIs, one endpoint per slice, mapped from the module's `MapEndpoints`.
* Route groups come from `MapModuleGroup(tag)`, which supplies the version prefix, the tag, the
  version set and `RequireAuthorization()`. Anonymous endpoints opt out explicitly with
  `.AllowAnonymous()`.
* Map literal routes before parameterised ones (`/users/search` before `/users/{id:guid}`), or the
  literal is shadowed.
* Every endpoint declares its permission with `.RequirePermission(...)` and its responses with
  `.Produces<T>()` / `.ProducesProblem()`. `AuthorizationMetadataTests` fails the build for an
  endpoint that declares neither a permission nor `AllowAnonymous`.
* A POST that creates something takes `.WithIdempotency()`.

## Commands and queries

* `ICommand<T>` and `IQuery<T>` from Mediator, defined in Contracts, one per slice.
* Validation is a `FluentValidation` validator per command, run by `ValidationBehavior<,>`. Handlers
  assume valid input.
* Handlers are sealed, take their dependencies through the primary constructor, and return DTOs —
  never entities.

## Errors

* Throw the domain exception (`NotFoundException`, `ConflictException`, `ForbiddenException`,
  `UnauthorizedException`) and stop. `GlobalExceptionHandler` is the **only** place an exception
  becomes a status code.
* Never catch broadly to turn an error into a response.
* Every response is RFC 9457 ProblemDetails and carries `correlationId` — the SPAs show it, and
  support asks for it.

## Permissions

* Declared per module in Contracts, registered from `ConfigureServices` into `PermissionConstants`.
* The JWT carries roles only. Permissions are resolved per request through `IPermissionProvider`, so
  a role edit takes effect on the next request rather than the next sign-in.
* `IsRoot` permissions are for the operator tenant. Only the root tenant's Admin role is seeded with
  them.
