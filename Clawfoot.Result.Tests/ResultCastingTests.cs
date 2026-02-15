using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

public class ResultCastingTests
{
    private const int TEST_VALUE = 5;

    private static Result<int> DoThingResult()
    {
        return TEST_VALUE;
    }

    [Fact]
    public void WithResult_AsGeneric_ReturnsGenericResult()
    {
        Result result = new Result();

        Result<int> converted = result.As<int>();

        converted.Success.ShouldBeTrue();
        result.Success.ShouldBeTrue();
    }

    [Fact]
    public void WithResult_InvokeResult_ReturnsResultWithValue()
    {
        Result result = new Result();

        Result<int> converted = result.InvokeResult(() => DoThingResult());

        converted.Value.ShouldBe(TEST_VALUE);
        converted.Success.ShouldBeTrue();
        result.Success.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
