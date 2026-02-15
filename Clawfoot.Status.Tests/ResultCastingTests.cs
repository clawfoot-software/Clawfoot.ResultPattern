using Shouldly;

namespace Clawfoot.Result.Tests;

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
    public void WithResult_CastsToGenericResult()
    {
        Result result = new Result();
        
        int resultValue = result.InvokeResult(() => DoThingResult());
        
        resultValue.ShouldBe(TEST_VALUE);
        result.Success.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}