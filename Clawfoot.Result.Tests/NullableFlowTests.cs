using System;
using System.Linq;
using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

/// <summary>
/// Positive flow tests: each dereferences Value without '!' after a check. The project treats nullable
/// warnings as errors, so if an annotation regresses these stop compiling. The negative cases (removing
/// the check produces a warning) are in Clawfoot.Result.Analyzers.Tests, which runs the compiler directly.
/// </summary>
public class NullableFlowTests
{
    private sealed class Principal
    {
        public string Name { get; init; } = "";
    }

    private static Result<Principal> Load(bool ok) =>
        ok ? new Principal { Name = "p" } : Result.Error("not found");

    private static Result PropagateHasErrors(bool ok, out string? name)
    {
        name = null;
        Result<Principal> result = Load(ok);
        if (result.HasErrors)
            return result;

        name = result.Value.Name;
        return Result.Ok();
    }

    [Fact]
    public void HasErrorsCheck_GivesNonNullValue()
    {
        PropagateHasErrors(true, out var name).Success.ShouldBeTrue();
        name.ShouldBe("p");
        PropagateHasErrors(false, out var none).HasErrors.ShouldBeTrue();
        none.ShouldBeNull();
    }

    [Fact]
    public void SuccessCheck_GivesNonNullValue()
    {
        Result<Principal> result = Load(true);
        if (!result.Success)
            throw new Exception("unexpected");
        result.Value.Name.ShouldBe("p");
    }

    [Fact]
    public void IsOkCheck_GivesNonNullValue()
    {
        Result<Principal> result = Load(true);
        if (result.IsOk)
            result.Value.Name.ShouldBe("p");
        else
            throw new Exception("unexpected");
    }

    [Fact]
    public void HasResultCheck_GivesNonNullValue()
    {
        Result<Principal> result = Load(true);
        if (result.HasResult)
            result.Value.Name.ShouldBe("p");
        else
            throw new Exception("unexpected");
    }

    [Fact]
    public void XunitAssertTrue_GivesNonNullValue()
    {
        Result<Principal> result = Load(true);
        Assert.True(result.Success);
        result.Value.Name.ShouldBe("p");
    }

    [Fact]
    public void XunitAssertFalse_HasErrors_GivesNonNullValue()
    {
        Result<Principal> result = Load(true);
        Assert.False(result.HasErrors);
        result.Value.Name.ShouldBe("p");
    }

    [Fact]
    public void GenericUnconstrained_HasErrorsCheck_GivesNonNullValue()
    {
        static string Describe<T>(Result<T> result) => result.HasErrors ? "error" : result.Value.ToString()!;

        Describe(new Result<int>(3)).ShouldBe("3");
        Describe(Result.Error<int>("e")).ShouldBe("error");
    }

    [Fact]
    public void TryGetValue_WhenSuccess_ReturnsNonNullValue()
    {
        if (Load(true).TryGetValue(out var principal))
            principal.Name.ShouldBe("p");
        else
            throw new Exception("unexpected");
    }

    [Fact]
    public void TryGetValue_WhenFailed_ReturnsFalse()
    {
        Load(false).TryGetValue(out var principal).ShouldBeFalse();
        principal.ShouldBeNull();
    }

    [Fact]
    public void TryGetValue_WhenFailedButCarryingValue_ReturnsFalse()
    {
        var failed = new Result<int>(5).WithError("e");
        failed.TryGetValue(out var value).ShouldBeFalse();
        value.ShouldBe(0);
    }

    [Fact]
    public void GetValueOrThrow_WhenSuccess_ReturnsValue()
    {
        Load(true).GetValueOrThrow().Name.ShouldBe("p");
    }

    [Fact]
    public void GetValueOrThrow_WhenFailed_ThrowsWithErrorMessages()
    {
        var result = Result.Error<int>("first").WithError("second");
        var ex = Should.Throw<InvalidOperationException>(() => result.GetValueOrThrow());
        ex.Message.ShouldContain("first");
        ex.Message.ShouldContain("second");
        ex.InnerException.ShouldBeNull();
    }

    [Fact]
    public void GetValueOrThrow_WhenFailedWithException_UsesItAsInner()
    {
        var cause = new TimeoutException("db");
        var ex = Should.Throw<InvalidOperationException>(() => Result.Error<int>(cause).GetValueOrThrow());
        ex.InnerException.ShouldBeSameAs(cause);
    }

    [Fact]
    public void GetValueOrThrow_WhenFailedWithExceptions_AggregatesThem()
    {
        var result = Result.Error<int>(new TimeoutException("a")).WithException(new TimeoutException("b"));
        var ex = Should.Throw<InvalidOperationException>(() => result.GetValueOrThrow());
        ex.InnerException.ShouldBeOfType<AggregateException>()
            .InnerExceptions.Select(e => e.Message).ShouldBe(new[] { "a", "b" });
    }

    [Fact]
    public void OkT_WithNull_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Result.Ok<string>(null!));
    }

    [Fact]
    public void OkT_WithDefaultValueType_IsAValue()
    {
        var result = Result.Ok(0);
        result.HasResult.ShouldBeTrue();
        result.Value.ShouldBe(0);
    }

    [Fact]
    public void NullableValueTypeArgument_WithNull_Throws()
    {
        int? none = null;
#pragma warning disable CFRESULT004 // the nullable type argument is what's under test
        Should.Throw<ArgumentNullException>(() => new Result<int?>(none!));
#pragma warning restore CFRESULT004
    }

    [Fact]
    public void ErrorT_WithEmptyErrors_Throws()
    {
        Should.Throw<ArgumentException>(() => Result.Error<int>(Array.Empty<IError>()));
    }

    [Fact]
    public void InvokeResult_Static_WhenDelegateReturnsNull_Throws()
    {
        // A null return breaks the contract; it is not a captured failure
        Should.Throw<ArgumentNullException>(() => Result.InvokeResult<string>(() => null!));
    }

    [Fact]
    public void InvokeResult_Static_WhenDelegateThrows_ReturnsError()
    {
        var result = Result.InvokeResult<string>(() => throw new InvalidOperationException("boom"));
        ResultTestHarness.AssertFailure(result, 1, "boom");
    }

    [Fact]
    public void InvokeResult_Extension_OnFailedResult_WhenDelegateReturnsNull_StaysFailed()
    {
        var failed = Result.Error("e");
        var result = failed.InvokeResult<string, Result>(() => null!);
        ResultTestHarness.AssertFailure(result, 1, "e");
    }

    [Fact]
    public void Deconstruct_GivesNullableValue()
    {
        var (result, value) = Load(false);
        result.HasErrors.ShouldBeTrue();
        value.ShouldBeNull();
    }
}
