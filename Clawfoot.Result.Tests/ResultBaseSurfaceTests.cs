using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

/// <summary>
/// Surface area tests for ResultBase and AbstractResult<T>, using Result as the concrete type.
/// </summary>
public class ResultBaseSurfaceTests
{
    [Fact]
    public void DefaultConstructor_CreatesSuccessResult()
    {
        var r = new Result();
        ResultTestHarness.AssertSuccess(r, "Success");
        ResultTestHarness.AssertNoExceptions(r);
    }

    [Fact]
    public void Constructor_WithSuccessMessage_SetsMessage()
    {
        var r = new Result(ResultTestHarness.SuccessMessage);
        ResultTestHarness.AssertSuccess(r, ResultTestHarness.SuccessMessage);
    }

    [Fact]
    public void Constructor_WithNullSuccessMessage_UsesDefaultMessage()
    {
        var r = new Result((string)null);
        ResultTestHarness.AssertSuccess(r, "Success");
    }

    [Fact]
    public void Constructor_WithWhitespaceSuccessMessage_UsesDefaultMessage()
    {
        var r = new Result("   ");
        ResultTestHarness.AssertSuccess(r, "Success");
    }

    [Fact]
    public void Constructor_WithErrors_CreatesFailedResult()
    {
        var errors = new[] { ResultTestHarness.SampleError };
        var r = new Result(errors, null, null);
        ResultTestHarness.AssertFailure(r, 1, ResultTestHarness.ErrorMessage);
        r.Message.ShouldContain("1 error");
    }

    [Fact]
    public void Constructor_WithErrorsAndSuccessMessage_StillFails_MessageShowsErrorCount()
    {
        var r = new Result(new[] { ResultTestHarness.SampleError }, null, ResultTestHarness.SuccessMessage);
        ResultTestHarness.AssertFailure(r, 1, ResultTestHarness.ErrorMessage);
        r.Message.ShouldContain("1 error");
    }

    [Fact]
    public void Constructor_WithExceptions_SetsHasExceptions()
    {
        var ex = ResultTestHarness.SampleException;
        var r = new Result(null, new[] { ex }, null);
        ResultTestHarness.AssertHasException(r, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void Constructor_WithNullErrors_UsesEmptyErrors()
    {
        var r = new Result((IEnumerable<IError>)null, null, null);
        ResultTestHarness.AssertSuccess(r);
    }

    [Fact]
    public void Message_WhenSuccess_ReturnsSuccessMessage()
    {
        var r = new Result(ResultTestHarness.SuccessMessage);
        r.Message.ShouldBe(ResultTestHarness.SuccessMessage);
    }

    [Fact]
    public void ToString_WhenNoErrors_ReturnsEmpty()
    {
        var r = new Result();
        r.ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public void ToString_WhenHasErrors_JoinsErrorMessages()
    {
        var r = new Result(new[] { new Error("A"), new Error("B") }, null, null);
        r.ToString().ShouldBe("A\nB");
        r.ToString("|").ShouldBe("A|B");
    }

    [Fact]
    public void ToUserFriendlyString_WhenNoErrors_ReturnsEmpty()
    {
        var r = new Result();
        r.ToUserFriendlyString().ShouldBe(string.Empty);
    }

    [Fact]
    public void ToUserFriendlyString_WhenHasErrors_JoinsUserMessages()
    {
        var r = new Result(new[] { new Error("dev", "user1"), new Error("dev2", "user2") }, null, null);
        r.ToUserFriendlyString("\n").ShouldBe("user1\nuser2");
    }

    [Fact]
    public void AsT_ReturnsNewResultT_WithSameState_NoValue()
    {
        var r = new Result(ResultTestHarness.SuccessMessage);
        var typed = r.As<int>();
        typed.ShouldNotBeNull();
        typed.Success.ShouldBeTrue();
        typed.Message.ShouldBe(ResultTestHarness.SuccessMessage);
        typed.HasResult.ShouldBeFalse();
        typed.Value.ShouldBe(default);
    }

    [Fact]
    public void AsT_WhenFailed_PreservesErrors()
    {
        var r = new Result(new[] { ResultTestHarness.SampleError }, null, null);
        var typed = r.As<string>();
        ResultTestHarness.AssertFailure(typed, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void SetResultT_ReturnsNewResultT_WithValue()
    {
        var r = new Result();
        var typed = r.SetResult(ResultTestHarness.SampleValue);
        typed.ShouldNotBeNull();
        ResultTestHarness.AssertUnchanged(r, true);
        typed.Success.ShouldBeTrue();
        typed.Value.ShouldBe(ResultTestHarness.SampleValue);
        typed.HasResult.ShouldBeTrue();
    }

    [Fact]
    public void WithException_ReturnsNewResult_OriginalUnchanged()
    {
        var r = new Result(ResultTestHarness.SuccessMessage);
        var withEx = r.WithException(ResultTestHarness.SampleException);
        ResultTestHarness.AssertNewInstance(r, withEx);
        ResultTestHarness.AssertUnchanged(r, true);
        ResultTestHarness.AssertFailure(withEx, 1, ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertHasException(withEx, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void WithError_String_ReturnsNewResult_OriginalUnchanged()
    {
        var r = new Result();
        var withErr = r.WithError(ResultTestHarness.ErrorMessage, ResultTestHarness.UserMessage);
        ResultTestHarness.AssertNewInstance(r, withErr);
        ResultTestHarness.AssertUnchanged(r, true);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void WithError_IError_ReturnsNewResult_OriginalUnchanged()
    {
        var r = new Result();
        var withErr = r.WithError(ResultTestHarness.SampleError);
        ResultTestHarness.AssertNewInstance<Result>(r, withErr);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void WithErrors_ReturnsNewResult_CombinesErrors()
    {
        var r = new Result(new[] { new Error("First") }, null, null);
        var withMore = r.WithErrors(new[] { new Error("Second") });
        ResultTestHarness.AssertNewInstance(r, withMore);
        ResultTestHarness.AssertFailure(withMore, 2);
        withMore.Errors.Select(e => e.Message).ShouldBe(new[] { "First", "Second" });
    }

    [Fact]
    public void WithErrorIf_WhenTrue_ReturnsNewResultWithError()
    {
        var r = new Result();
        var withErr = r.WithErrorIf(true, ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertNewInstance(r, withErr);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void WithErrorIf_WhenFalse_ReturnsSameResult()
    {
        var r = new Result();
        var unchanged = r.WithErrorIf(false, ResultTestHarness.ErrorMessage);
        unchanged.ShouldBeSameAs(r);
        ResultTestHarness.AssertSuccess(unchanged);
    }

    [Fact]
    public void WithErrorIfNull_ReferenceType_WhenNull_AddsError()
    {
        var r = new Result();
        string nil = null;
        var withErr = r.WithErrorIfNull(nil, ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void WithErrorIfNull_ReferenceType_WhenNotNull_ReturnsSame()
    {
        var r = new Result();
        var withOk = r.WithErrorIfNull("hello", ResultTestHarness.ErrorMessage);
        withOk.ShouldBeSameAs(r);
        ResultTestHarness.AssertSuccess(withOk);
    }

    [Fact]
    public void WithErrorIfNull_NullableStruct_WhenNull_AddsError()
    {
        var r = new Result();
        int? nil = null;
        var withErr = r.WithErrorIfNull(nil, ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void WithErrorIfNull_NullableStruct_WhenHasValue_ReturnsSame()
    {
        var r = new Result();
        int? value = 1;
        var withOk = r.WithErrorIfNull(value, ResultTestHarness.ErrorMessage);
        withOk.ShouldBeSameAs(r);
        ResultTestHarness.AssertSuccess(withOk);
    }

    [Fact]
    public void WithErrorIfNullOrDefault_ReferenceType_WhenNull_AddsError()
    {
        var r = new Result();
        string nil = null;
        var withErr = r.WithErrorIfNullOrDefault(nil, ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void WithErrorIfNullOrDefault_NullableStruct_WhenNull_AddsError()
    {
        var r = new Result();
        int? nil = null;
        var withErr = r.WithErrorIfNullOrDefault(nil, ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void WithErrorIfNullOrDefault_NullableStruct_WhenDefault_AddsError()
    {
        var r = new Result();
        int? zero = 0;
        var withErr = r.WithErrorIfNullOrDefault(zero, ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }
}
