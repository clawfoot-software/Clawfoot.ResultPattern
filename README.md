# Clawfoot.ResultPattern

A Results pattern library for .NET: return success/failure and propagate errors across layers without throwing. Use `Result` and `Result<T>` instead of exceptions for expected failure paths.

**Results are immutable.** Every operation returns a new instance; chain with `Invoke`, `WithError`, `WithValue`, or combine with `Result.Combine(...)`.

**Null-safe.** A successful `Result<T>` always carries a non-null value, and the library is fully annotated for nullable reference types: after a `HasErrors`/`Success` check the compiler knows `Value` is not null. No `!` needed.

**Target:** .NET Standard 2.1. Upgrading from 3.x? See [CHANGELOG.md](CHANGELOG.md#migrating-from-3x).

## Install

```bash
dotnet add package Clawfoot.ResultPattern
```

## Quick usage

```csharp
using Clawfoot.ResultPattern;

// Success (no value): a plain Result
Result result = Result.Ok();
if (!result) return result;

// Success with value: return the value, it converts implicitly
Result<User> FindUser(Guid id)
{
    User? user = _db.Find(id);
    if (user is null) return Result.Error("Not found", userMessage: "We couldn't find that user.");
    return user;
}

// Consuming: check, then use Value. The compiler knows it's not null.
Result<User> userResult = FindUser(id);
if (userResult.HasErrors)
    return userResult;              // or (Result)userResult to propagate into another Result<TOther>

Console.WriteLine(userResult.Value.Name);   // no '!'
```

## Results and values

| You have | Use |
|---|---|
| Success, no value | `Result` (`Result.Ok()`) |
| Success with a value | `Result<T>`: return the value, or `Result.Ok(value)` |
| Success where the value may legitimately be absent ("found nothing") | `(Result, T?)` tuple, or a plain `Result` plus an out/nullable value |
| Failure | `Result.Error(...)`, which converts to any `Result<T>` |

The rules, enforced by annotations, analyzers and at runtime:

- A **successful** `Result<T>` always has a non-null `Value`. Creating one with null throws `ArgumentNullException`, and the compiler warns first (`[DisallowNull]`).
- A **failed** result converts freely to any `Result<T>`, which is how errors propagate up the stack.
- A **successful** plain `Result` cannot become a `Result<T>`, because there is no value to put in it. `return Result.Ok();` in a `Result<T>` method is a compile error (`CFRESULT003`) and throws `InvalidOperationException` at runtime.

### Reading the value

```csharp
if (result.HasErrors) return result;   // or: if (!result.Success) / !result.IsOk / !result.HasResult
use(result.Value);

if (result.TryGetValue(out var value)) use(value);

// Tests and startup code: throws InvalidOperationException listing the errors
User user = CreateUser().GetValueOrThrow();
```

`Assert.True(result.Success)` and `Assert.False(result.HasErrors)` (xUnit) also tell the compiler the value is present. Fluent assertion chains like `result.HasErrors.Should().BeFalse()` don't, so use `GetValueOrThrow()` there.

## Immutable patterns

Assign the returned result to keep chaining or combining:

```csharp
// Add an error → new result (a Result<T> keeps its value)
result = result.WithError("Validation failed");

// Combine multiple results → new result (errors combined; for Result<T>, last successful value wins)
Result combined = Result.Combine(result1, result2);
Result<int> combinedT = Result.Combine(r1, r2, r3);
Result<User> withValidation = Result.Combine(validationResult, userResult);

// Chain Invoke (returns new result on exception)
result = result.Invoke(() => ValidateInput(input)).Invoke(() => SaveToDb(entity));

// InvokeResult returns Result<T>
Result<int> r = start.InvokeResult(() => GetCount());
```

## Error handling in a flow

Use `Invoke` / `InvokeAsync` to run code and capture exceptions into a **new** result (no mutation):

```csharp
var result = Result.Ok()
    .Invoke(() => ValidateInput(input))
    .Invoke(() => SaveToDb(entity));

if (!result) return result;

// Async
var result = await Result.Ok()
    .InvokeAsync(async () => await FetchAndSaveAsync());
```

## Enum-based errors

Decorate an enum with `[Error]` and create results from it:

```csharp
public enum UserErrors
{
    [Error(Code = 404, Message = "User not found", UserMessage = "We couldn't find that user.")]
    NotFound,
    [Error(Code = 400, Message = "Invalid id")]
    InvalidId
}

Result r = Result.FromError(UserErrors.NotFound);
```

## Analyzers

The package includes Roslyn analyzers. They load automatically, with no extra package to install.

| ID | Default | Flags |
|---|---|---|
| `CFRESULT001` | Warning | Discarded result of `With*`, `Invoke`, `InvokeAsync`, `Combine` (results are immutable, so the call does nothing) |
| `CFRESULT002` | Warning | `result.Value!`. Check `HasErrors`/`Success` instead, or use `GetValueOrThrow()` in tests |
| `CFRESULT003` | **Error** | A known-successful `Result` (`Result.Ok()`, `new Result()`) converted to `Result<T>` (implicitly, by cast, or with `As<T>()`) |
| `CFRESULT004` | Warning | `Result<X?>`: a nullable type argument can't represent "no value"; use `(Result, X?)` |

Raise or lower severities in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.CFRESULT002.severity = error
dotnet_diagnostic.CFRESULT004.severity = error
```

## Features

- **Result** and **Result<T>**: immutable; `Success`, `IsOk`, `HasErrors`, `HasResult`, `Errors`, `Message`, `Value`, `TryGetValue`, `GetValueOrThrow`
- **Nullable flow**: `HasErrors`/`Success`/`IsOk`/`HasResult`/`TryGetValue` tell the compiler when `Value` is non-null
- **Result.Combine(...)**: combines multiple results (errors combined; for `Result<T>`, last successful value wins)
- **WithError** / **WithErrors** / **WithException** / **WithValue**: return a new result with added state
- **Invoke** / **InvokeAsync** / **InvokeResult**: wrap calls and return a new result (chainable)
- **Error** and **IError**: structured errors (Code, Message, UserMessage, GroupName, MemberName)
- **ErrorAttribute** and **Error.From(enum)** for enum-driven error messages
- Implicit conversion of `Result` to `bool` (`if (!result) ...`)

## License

LGPL-3.0-only. © 2026 Douglas Gaskell / Clawfoot Software
