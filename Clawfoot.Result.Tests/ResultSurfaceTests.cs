using System;
using System.Collections.Generic;
using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

/// <summary>
/// Surface area tests for the Result class (constructors, static factories, Combine, Deconstruct, implicit bool).
/// </summary>
public class ResultSurfaceTests
{
    [Fact]
    public void Result_CopyConstructor_FromResult_CopiesSuccess()
    {
        var source = new Result(ResultTestHarness.SuccessMessage);
        var r = new Result(source);
        ResultTestHarness.AssertSuccess(r, ResultTestHarness.SuccessMessage);
    }

    [Fact]
    public void Result_CopyConstructor_FromFailedResult_CopiesErrors()
    {
        var source = Result.Error(ResultTestHarness.ErrorMessage);
        var r = new Result(source);
        ResultTestHarness.AssertFailure(r, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void Result_CopyConstructor_FromNull_ProducesEmptyResult()
    {
        var r = new Result((ResultBase)null);
        ResultTestHarness.AssertSuccess(r);
        r.Errors.ShouldBeEmpty();
        r.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    public void Ok_NoArgs_CreatesSuccessWithDefaultMessage()
    {
        var r = Result.Ok();
        ResultTestHarness.AssertSuccess(r, "Success");
    }

    [Fact]
    public void Ok_WithMessage_CreatesSuccessWithMessage()
    {
        var r = Result.Ok(ResultTestHarness.SuccessMessage);
        ResultTestHarness.AssertSuccess(r, ResultTestHarness.SuccessMessage);
    }

    [Fact]
    public void OkT_WithValue_CreatesSuccessResultT()
    {
        var r = Result.Ok(ResultTestHarness.SampleValue);
        r.Success.ShouldBeTrue();
        r.Value.ShouldBe(ResultTestHarness.SampleValue);
        r.HasResult.ShouldBeTrue();
    }

    [Fact]
    public void OkT_WithValueAndMessage_CreatesSuccessResultT()
    {
        var r = Result.Ok(ResultTestHarness.SampleValue, ResultTestHarness.SuccessMessage);
        r.Success.ShouldBeTrue();
        r.Value.ShouldBe(ResultTestHarness.SampleValue);
        r.Message.ShouldBe(ResultTestHarness.SuccessMessage);
    }

    [Fact]
    public void Error_String_CreatesFailedResult()
    {
        var r = Result.Error(ResultTestHarness.ErrorMessage, ResultTestHarness.UserMessage);
        ResultTestHarness.AssertFailure(r, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void Error_IError_CreatesFailedResult()
    {
        var r = Result.Error(ResultTestHarness.SampleError);
        ResultTestHarness.AssertFailure(r, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void Error_IEnumerable_CreatesFailedResultWithAllErrors()
    {
        var errors = new[] { new Error("A"), new Error("B") };
        var r = Result.Error(errors);
        ResultTestHarness.AssertFailure(r, 2);
    }

    [Fact]
    public void Error_Exception_CreatesFailedResultWithException()
    {
        var ex = ResultTestHarness.SampleException;
        var r = Result.Error(ex);
        ResultTestHarness.AssertFailure(r, 1, ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertHasException(r, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void ErrorT_String_CreatesFailedResultT()
    {
        var r = Result.Error<int>(ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertFailure(r, 1, ResultTestHarness.ErrorMessage);
        r.HasResult.ShouldBeFalse();
    }

    [Fact]
    public void ErrorT_Exception_CreatesFailedResultT()
    {
        var r = Result.Error<int>(ResultTestHarness.SampleException);
        ResultTestHarness.AssertFailure(r, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void Combine_TwoResults_CombinesErrors_LastMessageWhenSuccess()
    {
        var r1 = Result.Ok("msg1");
        var r2 = Result.Ok("msg2");
        var combined = Result.Combine(r1, r2);
        ResultTestHarness.AssertSuccess(combined, "msg2");
    }

    [Fact]
    public void Combine_TwoResults_WhenOneFails_CombinedFails()
    {
        var r1 = Result.Ok();
        var r2 = Result.Error(ResultTestHarness.ErrorMessage);
        var combined = Result.Combine(r1, r2);
        ResultTestHarness.AssertFailure(combined, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void Combine_TwoResults_CombinesAllErrors()
    {
        var r1 = Result.Error("E1");
        var r2 = Result.Error("E2");
        var combined = Result.Combine(r1, r2);
        ResultTestHarness.AssertFailure(combined, 2);
        combined.Errors.ShouldContain(e => e.Message == "E1");
        combined.Errors.ShouldContain(e => e.Message == "E2");
    }

    [Fact]
    public void Combine_WithParams_CombinesAll()
    {
        var combined = Result.Combine(Result.Error("A"), Result.Error("B"), Result.Error("C"));
        ResultTestHarness.AssertFailure(combined, 3);
    }

    [Fact]
    public void Combine_IEnumerable_WhenNull_ReturnsNewEmptyResult()
    {
        IEnumerable<Result> nullEnumerable = null;
        var combined = Result.Combine(nullEnumerable);
        ResultTestHarness.AssertSuccess(combined);
    }

    [Fact]
    public void Combine_IEnumerable_WhenEmpty_ReturnsNewEmptyResult()
    {
        var combined = Result.Combine((IEnumerable<Result>)Array.Empty<Result>());
        ResultTestHarness.AssertSuccess(combined);
    }

    [Fact]
    public void Combine_ResultBaseParams_CombinesMixedResults()
    {
        var r1 = Result.Error("E1");
        var r2 = new Result<int>(99);
        var combined = Result.Combine(new ResultBase[] { r1, r2 });
        ResultTestHarness.AssertFailure(combined, 1, "E1");
    }

    [Fact]
    public void Combine_ResultBaseParams_WhenNull_ReturnsNewEmptyResult()
    {
        var combined = Result.Combine((ResultBase[])null);
        ResultTestHarness.AssertSuccess(combined);
    }

    [Fact]
    public void Combine_ResultBaseParams_WhenEmpty_ReturnsNewEmptyResult()
    {
        var combined = Result.Combine(Array.Empty<ResultBase>());
        ResultTestHarness.AssertSuccess(combined);
    }

    [Fact]
    public void Deconstruct_OutSuccessAndResult()
    {
        var r = Result.Ok(ResultTestHarness.SuccessMessage);
        var (success, result) = r;
        success.ShouldBeTrue();
        result.ShouldBeSameAs(r);
    }

    [Fact]
    public void Deconstruct_WhenFailed_OutSuccessFalse()
    {
        var r = Result.Error(ResultTestHarness.ErrorMessage);
        var (success, result) = r;
        success.ShouldBeFalse();
        result.ShouldBeSameAs(r);
    }

    [Fact]
    public void ImplicitBool_WhenSuccess_True()
    {
        Result r = Result.Ok();
        bool b = r;
        b.ShouldBeTrue();
    }

    [Fact]
    public void ImplicitBool_WhenFailed_False()
    {
        Result r = Result.Error(ResultTestHarness.ErrorMessage);
        bool b = r;
        b.ShouldBeFalse();
    }

    [Fact]
    public void ImplicitBool_WhenNull_False()
    {
        Result r = null;
        bool b = r;
        b.ShouldBeFalse();
    }
}
