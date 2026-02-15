using System;
using System.Collections.Generic;
using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

/// <summary>
/// Surface area tests for Result&lt;T&gt; (constructors, WithValue, HasResult, To, conversions, Combine, Deconstruct).
/// </summary>
public class ResultGenericSurfaceTests
{
    [Fact]
    public void DefaultConstructor_NoValue_HasResultFalse()
    {
        var r = new Result<int>();
        r.Success.ShouldBeTrue();
        r.HasResult.ShouldBeFalse();
        r.Value.ShouldBe(0);
    }

    [Fact]
    public void Constructor_WithValue_SetsValue()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue, ResultTestHarness.SuccessMessage);
        r.Success.ShouldBeTrue();
        r.Value.ShouldBe(ResultTestHarness.SampleValue);
        r.HasResult.ShouldBeTrue();
        r.Message.ShouldBe(ResultTestHarness.SuccessMessage);
    }

    [Fact]
    public void Constructor_WithErrors_HasResultFalse()
    {
        var r = new Result<int>(new[] { ResultTestHarness.SampleError }, null, null);
        ResultTestHarness.AssertFailure(r, 1);
        r.HasResult.ShouldBeFalse();
    }

    [Fact]
    public void WithValue_ReturnsNewResult_OriginalUnchanged()
    {
        var r = new Result<int>(1);
        var withValue = r.WithValue(99);
        ResultTestHarness.AssertNewInstance(r, withValue);
        r.Value.ShouldBe(1);
        withValue.Value.ShouldBe(99);
    }

    [Fact]
    public void WithValue_WhenFailed_PreservesErrors()
    {
        var r = Result.Error<int>(ResultTestHarness.ErrorMessage);
        var withValue = r.WithValue(ResultTestHarness.SampleValue);
        ResultTestHarness.AssertFailure(withValue, 1, ResultTestHarness.ErrorMessage);
        withValue.Value.ShouldBe(ResultTestHarness.SampleValue);
    }

    [Fact]
    public void ToTResult_ReturnsNewResultTResult_WithValue()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue);
        var asString = r.To("forty-two");
        asString.ShouldNotBeNull();
        asString.Success.ShouldBeTrue();
        asString.Value.ShouldBe("forty-two");
    }

    [Fact]
    public void ToTResult_WhenFailed_PreservesErrors()
    {
        var r = Result.Error<int>(ResultTestHarness.ErrorMessage);
        var asString = r.To("ignored");
        ResultTestHarness.AssertFailure(asString, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void Implicit_FromT_CreatesSuccessResultT()
    {
        Result<int> r = ResultTestHarness.SampleValue;
        r.Success.ShouldBeTrue();
        r.Value.ShouldBe(ResultTestHarness.SampleValue);
    }

    [Fact]
    public void Implicit_ToResult_FromResultT()
    {
        Result<int> r = new Result<int>(ResultTestHarness.SampleValue);
        Result baseResult = r;
        baseResult.ShouldNotBeNull();
        baseResult.Success.ShouldBeTrue();
    }

    [Fact]
    public void Implicit_FromResult_ToResultT()
    {
        Result r = Result.Ok();
        Result<int> typed = r;
        typed.Success.ShouldBeTrue();
        typed.HasResult.ShouldBeFalse();
    }

    [Fact]
    public void Deconstruct_OutResultAndValue()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue);
        var (result, value) = r;
        result.Success.ShouldBe(r.Success);
        value.ShouldBe(ResultTestHarness.SampleValue);
    }

    [Fact]
    public void Deconstruct_OutResultValueAndSuccess()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue);
        var (result, value, success) = r;
        result.Success.ShouldBe(r.Success);
        value.ShouldBe(ResultTestHarness.SampleValue);
        success.ShouldBeTrue();
    }

    [Fact]
    public void Combine_ResultT_TwoSuccess_LastWithValueWins()
    {
        var r1 = new Result<int>(1);
        var r2 = new Result<int>(2);
        var combined = Result.Combine(r1, r2);
        combined.Success.ShouldBeTrue();
        combined.Value.ShouldBe(2);
    }

    [Fact]
    public void Combine_ResultT_WhenOneFails_ValueFromLastWithValue()
    {
        var r1 = new Result<int>(1);
        var r2 = Result.Error<int>("E");
        var r3 = new Result<int>(3);
        var combined = Result.Combine<int>(r1, r2, r3);
        ResultTestHarness.AssertFailure(combined, 1, "E");
        combined.Value.ShouldBe(3);
    }

    [Fact]
    public void Combine_ResultT_IEnumerable_WhenNull_ReturnsNewEmptyResultT()
    {
        var combined = Result.Combine<int>(null);
        combined.Success.ShouldBeTrue();
        combined.HasResult.ShouldBeFalse();
    }

    [Fact]
    public void Combine_ResultBase_And_ResultT_CombinesErrors_ValueFromTyped()
    {
        var baseResult = Result.Error("BaseError");
        var typed = new Result<int>(ResultTestHarness.SampleValue);
        var combined = Result.Combine<int>(baseResult, typed);
        ResultTestHarness.AssertFailure(combined, 1, "BaseError");
        combined.Value.ShouldBe(ResultTestHarness.SampleValue);
    }

    [Fact]
    public void Combine_ResultBase_And_ResultT_WhenBaseNull_ValueFromTyped()
    {
        ResultBase baseResult = null;
        var typed = new Result<int>(ResultTestHarness.SampleValue);
        var combined = Result.Combine<int>(baseResult, typed);
        combined.Success.ShouldBeTrue();
        combined.Value.ShouldBe(ResultTestHarness.SampleValue);
    }

    [Fact]
    public void ResultT_WithError_ReturnsNewInstance_OriginalUnchanged()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue);
        var withErr = r.WithError(ResultTestHarness.ErrorMessage);
        ResultTestHarness.AssertNewInstance(r, withErr);
        r.Success.ShouldBeTrue();
        r.Value.ShouldBe(ResultTestHarness.SampleValue);
        ResultTestHarness.AssertFailure(withErr, 1, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void ResultT_WithException_ReturnsNewInstance_OriginalUnchanged()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue);
        var withEx = r.WithException(ResultTestHarness.SampleException);
        ResultTestHarness.AssertNewInstance(r, withEx);
        ResultTestHarness.AssertFailure(withEx, 1);
        ResultTestHarness.AssertHasException(withEx, ResultTestHarness.ErrorMessage);
    }

    [Fact]
    public void HasResult_WhenSuccessAndValueTypeDefault_False()
    {
        var r = new Result<int>(0);
        r.Success.ShouldBeTrue();
        r.HasResult.ShouldBeFalse();
    }

    [Fact]
    public void HasResult_WhenSuccessAndReferenceTypeNull_False()
    {
        var r = new Result<string>((string)null);
        r.Success.ShouldBeTrue();
        r.HasResult.ShouldBeFalse();
    }
}
