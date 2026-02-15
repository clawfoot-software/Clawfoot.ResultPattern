using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

/// <summary>
/// Shared test data and assertion helpers to reduce boilerplate in Result/ResultBase tests.
/// </summary>
public static class ResultTestHarness
{
    public const string SuccessMessage = "Done";
    public const string ErrorMessage = "Something failed";
    public const string UserMessage = "User-friendly message";
    public const int SampleValue = 42;

    public static IError SampleError => new Error(ErrorMessage, UserMessage);
    public static Exception SampleException => new InvalidOperationException(ErrorMessage);

    /// <summary>
    /// Asserts result is success and optionally checks message.
    /// </summary>
    public static void AssertSuccess(ResultBase result, string? expectedMessage = null)
    {
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.IsOk.ShouldBeTrue();
        result.HasErrors.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
        if (expectedMessage != null)
            result.Message.ShouldBe(expectedMessage);
    }

    /// <summary>
    /// Asserts result is failure with expected error count and optionally first error message.
    /// </summary>
    public static void AssertFailure(ResultBase result, int expectedErrorCount = 1, string? firstErrorMessage = null)
    {
        result.ShouldNotBeNull();
        result.Success.ShouldBeFalse();
        result.IsOk.ShouldBeFalse();
        result.HasErrors.ShouldBeTrue();
        result.Errors.Count().ShouldBe(expectedErrorCount);
        if (firstErrorMessage != null)
            result.Errors.First().Message.ShouldBe(firstErrorMessage);
    }

    /// <summary>
    /// Asserts the result was not mutated: original still has the given success state.
    /// </summary>
    public static void AssertUnchanged(ResultBase original, bool expectedSuccess)
    {
        original.Success.ShouldBe(expectedSuccess);
    }

    /// <summary>
    /// Asserts result has no exceptions.
    /// </summary>
    public static void AssertNoExceptions(ResultBase result)
    {
        result.HasExceptions.ShouldBeFalse();
        result.Exceptions.ShouldBeEmpty();
    }

    /// <summary>
    /// Asserts result has exactly one exception with the given message.
    /// </summary>
    public static void AssertHasException(ResultBase result, string expectedMessage)
    {
        result.HasExceptions.ShouldBeTrue();
        result.Exceptions.Count().ShouldBe(1);
        result.Exceptions.First().Message.ShouldBe(expectedMessage);
    }

    /// <summary>
    /// Asserts two results are different instances (immutability).
    /// </summary>
    public static void AssertNewInstance<T>(T original, T returned) where T : class
    {
        returned.ShouldNotBeNull();
        returned.ShouldNotBeSameAs(original);
    }
}
