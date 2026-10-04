# Changelog

## 4.1.0 (unreleased)

Errors can carry a **kind**: an enum value for handling errors centrally, e.g. in one `switch`. Any enum works, so
applications can define their own kinds; a built-in `ErrorKind` covers the HTTP 4xx/5xx status codes.

### Added

- `IError.Kind` (`Enum?`). It's a default interface member returning null, so existing `IError` implementations keep compiling.
- `ErrorKind`: built-in kinds for the HTTP 4xx and 5xx status codes. Each value is its status code and names match
  `System.Net.HttpStatusCode`.
- `Error.Kind`, and a constructor that takes it: `new Error(message, kind, userMessage, code, groupName, memberName)`.
- `[Error(Kind = ...)]`: `Error.From` / `Result.FromError` copy it onto the error. A non-enum value throws `InvalidOperationException`.
- Kind overloads: `Result.Error(message, kind, ...)`, `Result.Error<T>(message, kind, ...)`, `WithError(message, kind, ...)`,
  `WithErrorIf` / `WithErrorIfNull` / `WithErrorIfNullOrDefault(..., message, kind, ...)`, `WithException(ex, kind)`,
  `Result.Error(ex, kind)`, `Result.Error<T>(ex, kind)`.
- On results: `GetErrorKind()` (first error with a kind), `TryGetErrorKind<TKind>(out kind)` (first kind of that enum
  type), `HasErrorKind(kind)` (any error with that kind; type and value must match).
- `ErrorKindJsonConverter` for System.Text.Json. Kinds are written as `"TypeName.Member"` with no setup; register your
  enum types to read them back. Unknown kinds throw `JsonException` unless `ignoreUnknownKinds: true`.
  `Error` now deserializes (its kind constructor is the `[JsonConstructor]`).
- Analyzer `CFRESULT005` (disabled by default): reports errors created without a kind. Enable it with
  `dotnet_diagnostic.CFRESULT005.severity = warning`.

### Changed

- Errors created from exceptions (`Result.Error(ex)`, `WithException(ex)`, and exceptions caught by `Invoke*` /
  `InvokeResult*`, including with `keepException: false`) have kind `ErrorKind.InternalServerError`.
- The package now depends on `System.Text.Json` (provided by the framework on .NET 5+).
- `new Error("message", null)` no longer compiles: `null` matches both the `userMessage` and the `kind` constructor.
  Name the argument (`userMessage: null`) or pass `""`. `Result.Error("message", null)` and `WithError("message", null)`
  still bind to the message-only overloads.

## 4.0.0

A successful `Result<T>` now always carries a non-null value, and the library is fully annotated for nullable
reference types. After `if (result.HasErrors) return ...;` the compiler knows `result.Value` is not null, so callers
no longer need `!`, and removing the check produces a nullable warning.

### Breaking changes

- **Successful `Result<T>` must carry a value.** Every path that used to produce "success, Value == null" now throws:
  - `new Result<T>(null)`, `Result.Ok<T>(null)`, implicit `T → Result<T>` from null: `ArgumentNullException`
  - `WithValue(null)`, `SetResult(null)` and `To(null)` on a successful result: `ArgumentNullException`
    (on a failed result they're allowed and leave it without a value)
  - implicit `Result → Result<T>` conversion and `As<T>()` on a **successful** result: `InvalidOperationException`.
    Failed results still convert, so error propagation is unchanged.
  - `Result<T>.As<TOther>()` on a successful result: `InvalidOperationException` (it would drop the value)
  - `new Result<T>(errors)` / `Result.Error<T>(errors)` with an empty error list: `ArgumentException`
  - `Result.Combine<T>(IEnumerable<Result<T>>)` with null (`ArgumentNullException`) or an empty sequence (`ArgumentException`).
    `Result.Combine<T>(ResultBase, Result<T>)` with a null typed result: `ArgumentNullException`.
  - `Result.InvokeResult` / `InvokeResultAsync` (static and extension) when the delegate returns null on a successful
    path: `ArgumentNullException`. This is no longer captured as an error result.
- **`Result<T>` has no parameterless constructor.** Start from a value (`new Result<T>(value)`), an error
  (`Result.Error<T>(...)`), or accumulate errors in a plain `Result` and convert it once it has failed.
- **`HasResult` treats default values as values.** `Result<int>` holding `0` and `Result<bool>` holding `false` now
  report `HasResult == true`, and `Combine<T>` keeps them instead of skipping them. On a `Result<T>`, `HasResult` is now
  equivalent to `Success`.
- **`WithError` / `WithErrors` / `WithException` keep the value** of a `Result<T>`. Previously they dropped it, so
  `WithErrors(emptyList)` turned a value result into "success, no value". A failed result's `Value` now returns the
  value it had before the error, matching `Combine` and `WithValue`.
- **`Value` is declared `T?`** and `Deconstruct` outputs `T?`. Reads without a preceding check now warn (CS8602/CS8600).
- **New analyzer error `CFRESULT003`**: `Result.Ok()`, `Result.Ok("msg")`, `new Result()` or `new Result("msg")`
  converted to `Result<T>` (implicitly, by cast, or with `As<T>()`) fails the build.
- `Error` string properties are never null: the parameterless constructor and `Error.From` (for `[Error]` attributes
  without a message) now use `string.Empty`. `Error.From` on an undefined enum value throws
  `InvalidOperationException` instead of `ArgumentNullException`.
- Assembly version now follows the package version (`4.0.0.0`; previously `0.3.2.x`).

### Added

- Nullable annotations across the public API (`Nullable` enabled, C# 12).
- `[MemberNotNullWhen]` flow on `Result<T>.HasErrors` (false ⇒ value), `Success`, `IsOk` and `HasResult`
  (true ⇒ value). `Result<T>` redeclares `HasErrors`/`Success`/`IsOk` with `new` so the attributes can name `Value`.
- `[DisallowNull]` on every success producer: the value constructor, `Result.Ok<T>`, implicit `T → Result<T>`,
  `WithValue`, `SetResult`, `To`. Passing a maybe-null value warns at compile time.
- `bool Result<T>.TryGetValue(out T? value)`: true and a non-null value when successful.
- `T Result<T>.GetValueOrThrow()`: the value, or `InvalidOperationException` listing the errors (inner exception: the
  result's exception, or an `AggregateException` of them). For tests and startup code.
- `Result.Combine<T>(Result, Result<T>)`. Without it, C# bound `Combine(plainResult, typedResult)` to
  `Combine<T>(Result<T>, Result<T>)` through the implicit conversion, which throws for a successful plain result.
- Analyzer rules (shipped in the package):
  - `CFRESULT002` (warning): `!` on `Result<T>.Value`.
  - `CFRESULT003` (error): a known-successful `Result` converted to `Result<T>`.
  - `CFRESULT004` (warning): `Result<X?>`, a nullable type argument.
- The package README is included in the NuGet package.

### Fixed

- Malformed XML doc comments (raw `Result<T>` in summaries) that broke IntelliSense text.
- README enum example: `[Error]` belongs on the enum members, not the enum type.

### Migrating from 3.x

| 3.x | 4.0 |
|---|---|
| `if (r.HasErrors) return r; var x = r.Value!;` | `if (r.HasErrors) return r; var x = r.Value;` |
| `Foo x = Create(...).Value!;` (tests) | `Foo x = Create(...).GetValueOrThrow();` |
| `r.HasErrors.Should().BeFalse(); r.Value!.Name` | `r.GetValueOrThrow().Name` (or `Assert.False(r.HasErrors)`) |
| `return Result.Ok();` in a `Result<T>` method | return a value, or change the return type to `Result` (`CFRESULT003`) |
| `Result<Foo?>` meaning "succeeded, maybe nothing" | `(Result, Foo?)` (`CFRESULT004`) |
| `var acc = new Result<T>(); ... acc = acc.WithError(...)` | accumulate in a plain `Result`, then `return acc;` once it has failed |

## 3.2.1

- `CFRESULT001` also flags discarded `Invoke`, `InvokeAsync` and `Combine` results.
