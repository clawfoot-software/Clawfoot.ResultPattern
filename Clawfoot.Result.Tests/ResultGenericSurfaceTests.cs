using System;
using System.Collections.Generic;
using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

/// <summary>
/// Surface area tests for Result<T> (constructors, WithValue, HasResult, To, conversions, Combine, Deconstruct).
/// </summary>
public class ResultGenericSurfaceTests
{
    [Fact]
    public void Constructor_WithNullValue_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new Result<string>((string)null!));
    }

    [Fact]
    public void Constructor_WithEmptyErrors_Throws()
    {
        // No value and no errors would be a value-less success
        Should.Throw<ArgumentException>(() => new Result<int>(Array.Empty<IError>()));
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
    public void Implicit_FromSuccessfulResult_ToResultT_Throws()
    {
        Result r = Result.Ok();
        Should.Throw<InvalidOperationException>(() =>
        {
            Result<int> typed = r;
            return typed;
        });
    }

    [Fact]
    public void Implicit_FromFailedResult_ToResultT_PropagatesErrors()
    {
        Result r = Result.Error(ResultTestHarness.ErrorMessage);
        Result<string> typed = r;
        ResultTestHarness.AssertFailure(typed, 1, ResultTestHarness.ErrorMessage);
        typed.HasResult.ShouldBeFalse();
    }

    [Fact]
    public void Implicit_FromNullResult_ToResultT_IsNull()
    {
        Result? r = null;
        Result<int>? typed = r;
        typed.ShouldBeNull();
    }

    [Fact]
    public void Implicit_FromNullValue_Throws()
    {
        string? value = null;
        Should.Throw<ArgumentNullException>(() =>
        {
            Result<string> typed = value!;
            return typed;
        });
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
    public void Combine_ResultT_IEnumerable_WhenNull_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Result.Combine<int>((IEnumerable<Result<int>>)null!));
    }

    [Fact]
    public void Combine_ResultT_IEnumerable_WhenEmpty_Throws()
    {
        // Nothing to take a value from
        Should.Throw<ArgumentException>(() => Result.Combine<int>(Enumerable.Empty<Result<int>>()));
    }

    [Fact]
    public void Combine_ResultT_KeepsDefaultValues()
    {
        var combined = Result.Combine(new Result<bool>(true), new Result<bool>(false));
        combined.Success.ShouldBeTrue();
        combined.Value.ShouldBeFalse();
    }

    [Fact]
    public void Combine_ResultT_AllFailed_NoValue()
    {
        var combined = Result.Combine(Result.Error<string>("E1"), Result.Error<string>("E2"));
        ResultTestHarness.AssertFailure(combined, 2, "E1");
        combined.HasResult.ShouldBeFalse();
        combined.Value.ShouldBeNull();
    }

    [Fact]
    public void Combine_ResultBase_And_ResultT_WhenTypedNull_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Result.Combine<int>(Result.Ok(), null!));
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
        ResultBase? baseResult = null;
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
    public void HasResult_WhenSuccessAndValueTypeDefault_True()
    {
        // 0 and false are values, not "no value"
        new Result<int>(0).HasResult.ShouldBeTrue();
        new Result<bool>(false).HasResult.ShouldBeTrue();
    }

    [Fact]
    public void HasResult_WhenFailedWithValue_False()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue).WithError(ResultTestHarness.ErrorMessage);
        r.HasResult.ShouldBeFalse();
    }

    [Fact]
    public void WithError_PreservesValue()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue);
        var withErr = r.WithError(ResultTestHarness.ErrorMessage);
        withErr.Value.ShouldBe(ResultTestHarness.SampleValue);
    }

    [Fact]
    public void WithErrors_Empty_KeepsSuccessAndValue()
    {
        // Previously dropped the value, producing a success without one
        var r = new Result<string>("value");
        var same = r.WithErrors(Array.Empty<IError>());
        same.Success.ShouldBeTrue();
        same.Value.ShouldBe("value");
    }

    [Fact]
    public void WithValue_WhenSuccessfulAndNull_Throws()
    {
        var r = new Result<string>("value");
        Should.Throw<ArgumentNullException>(() => r.WithValue(null!));
    }

    [Fact]
    public void To_WhenSuccessfulAndNull_Throws()
    {
        var r = new Result<int>(ResultTestHarness.SampleValue);
        Should.Throw<ArgumentNullException>(() => r.To<string>(null!));
    }

    [Fact]
    public void AsOtherT_WhenSuccessful_Throws()
    {
        // Converting would drop the value
        var r = new Result<int>(ResultTestHarness.SampleValue);
        Should.Throw<InvalidOperationException>(() => r.As<string>());
    }

    [Fact]
    public void AsOtherT_WhenFailed_PropagatesErrors()
    {
        var r = Result.Error<int>(ResultTestHarness.ErrorMessage);
        var other = r.As<string>();
        ResultTestHarness.AssertFailure(other, 1, ResultTestHarness.ErrorMessage);
    }
}
