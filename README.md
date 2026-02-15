# Clawfoot.ResultPattern

A Results pattern library for .NET: return success/failure and propagate errors across layers without throwing. Use `Result` and `Result<T>` instead of exceptions for expected failure paths.

**Results are immutable.** Every operation returns a new instance; chain with `Invoke`, `WithError`, `WithValue`, or combine with `Result.Combine(...)`.

**Target:** .NET Standard 2.1

## Install

```bash
dotnet add package Clawfoot.ResultPattern
```

## Quick usage

```csharp
using Clawfoot.ResultPattern;

// Success (no value)
Result result = Result.Ok();
if (!result) return result;

// Success with value
Result<User> userResult = Result.Ok(user, "Found.");
if (userResult.Success)
    Console.WriteLine(userResult.Value.Name);

// Failure (immutable: each factory returns a new result)
Result err = Result.Error("Not found", userMessage: "We couldn't find that.");
Result<User> errT = Result.Error<User>("Invalid id");
```

## Immutable patterns

Assign the returned result to keep chaining or combining:

```csharp
// Add an error → new result
result = result.WithError("Validation failed");

// Combine multiple results → new result (errors combined; for Result<T>, last value wins)
Result combined = Result.Combine(result1, result2);
Result<int> combinedT = Result.Combine(r1, r2, r3);

// Chain Invoke (returns new result on exception)
result = result.Invoke(() => ValidateInput(input)).Invoke(() => SaveToDb(entity));

// InvokeResult returns Result<T>; use .Value to get the value
Result<int> r = start.InvokeResult(() => GetCount());
int value = r.Value;
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
[Error(Code = 404, Message = "User not found", UserMessage = "We couldn't find that user.")]
public enum UserErrors { NotFound, InvalidId }

Result r = Result.FromError(UserErrors.NotFound);
```

## Features

- **Result** and **Result<T>** — immutable; `Success`, `HasErrors`, `Errors`, `Message`, `Value` (read-only)
- **Result.Combine(...)** — static method to combine multiple results (errors combined; for `Result<T>`, last value wins)
- **WithError** / **WithErrors** / **WithException** / **WithValue** — return a new result with added state
- **Invoke** / **InvokeAsync** / **InvokeResult** — wrap calls and return a new result (chainable)
- **Error** and **IError** — structured errors (Code, Message, UserMessage, GroupName); Error properties are read-only after creation
- **ErrorAttribute** and **Error.From(enum)** for enum-driven error messages
- Implicit conversion to `bool` (`if (!result) ...`)

## License

LGPL-3.0-only — © 2026 Douglas Gaskell / Clawfoot Software
