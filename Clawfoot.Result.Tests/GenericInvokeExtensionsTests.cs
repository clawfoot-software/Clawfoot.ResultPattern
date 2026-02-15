using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

// Largely testing for build errors around ambiguous methods
public class GenericInvokeExtensionsTests
{
    private const int TEST_VALUE = 5;

    private static Result<int> DoThingResult()
    {
        return TEST_VALUE;
    }

    private static Result<int> DoThingIResult()
    {
        return Result.Ok(TEST_VALUE);
    }

    [Fact]
    public void Result_WithResult_Ensure_NoAmbiguousInvoke()
    {
        Result<int> result = new Result<int>();

        Result<int> combined = result.InvokeResult(() => DoThingResult());
        int resultValue = combined.Value;

        resultValue.ShouldBe(TEST_VALUE);
    }

    [Fact]
    public void IResult_WithResult_Ensure_NoAmbiguousInvoke()
    {
        Result<int> result = new Result<int>();

        Result<int> combined = result.InvokeResult(() => DoThingIResult());
        int resultValue = combined.Value;

        resultValue.ShouldBe(TEST_VALUE);
    }

    [Fact]
    public void ResultT_Invoke()
    {
        var result = new Result<int>();

        Result<int> combined = result.InvokeResult(() => DoThingIResult());
        (Result r, int resultValue) = combined;

        resultValue.ShouldBe(TEST_VALUE);
    }

    [Fact]
    public void Result_Invoke()
    {
        var result = new Result();

        Result combined = result.Invoke(() => DoThingIResult());

        combined.Success.ShouldBeTrue();
        combined.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task InvokeResultAsync_WithReturningTaskResultResult_ReturnsResultResult()
    {
        static Task<Result<int>> DoThingResultAsync()
        {
            return Task.FromResult<Result<int>>(TEST_VALUE);
        }

        Result<int> result = await Result.InvokeResultAsync(async () => await DoThingResultAsync());

        result.Value.ShouldBe(TEST_VALUE);
        result.HasErrors.ShouldBeFalse();
    }
}
