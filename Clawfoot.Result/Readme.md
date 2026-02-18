# Clawfoot.ResultPattern

A Results pattern library for .NET: return success/failure and propagate errors across layers without throwing. Use `Result` and `Result<T>` instead of exceptions for expected failure paths.

**Target:** .NET Standard 2.1

## Install

```bash
dotnet add package Clawfoot.ResultPattern
```

## Quick usage

```csharp
using Clawfoot.ResultPattern;

// Success (no value) — e.g. from a method that returns Result
Result result = SaveSomething();
if (!result) return result;  // propagate failure

// Success with value
Result<User> userResult = Result.Ok(user, "Found.");
if (userResult.Success)
    Console.WriteLine(userResult.Value.Name);

// Failure
Result err = Result.Error("Not found", userMessage: "We couldn't find that.");
Result<User> errT = Result.Error<User>("Invalid id");
```

## Error handling in a flow

Use `Invoke` / `InvokeAsync` to run code and capture exceptions into the result instead of throwing:

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

- **Result** and **Result<T>** with `Success`, `HasErrors`, `Errors`, `Message`, `Value`
- **Merge** results to combine errors and optional values from multiple steps
- **Invoke** / **Do** and **InvokeAsync** to wrap calls and add errors/exceptions to the current result
- **Error** and **IError** for structured errors (Code, Message, UserMessage, GroupName)
- **ErrorAttribute** and **Error.From(enum)** for enum-driven error messages
- Implicit conversion to `bool` (`if (!result) ...`)

## Analyzer (CFRESULT001)

The package includes a Roslyn analyzer that warns when a `With*` method (`WithError`, `WithValue`, `WithException`, etc.) is called without using the return value. These methods return a new result; discarding it is usually a bug.

```csharp
// Warning CFRESULT001: return value must be used
result.WithError("something went wrong");

// OK: assign or return
var updated = result.WithError("something went wrong");
return result.WithError("something went wrong");
```

To suppress in rare cases: `#pragma warning disable CFRESULT001`

## License

LGPL-3.0-only — © 2026 Douglas Gaskell / Clawfoot Software
