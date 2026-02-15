using Shouldly;

namespace Clawfoot.Result.Tests;

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
        
        int resultValue = result.InvokeResult(() => DoThingResult());
        
        resultValue.ShouldBe(TEST_VALUE);
    }
    
    [Fact]
    public void IResult_WithResult_Ensure_NoAmbiguousInvoke()
    {
        Result<int> result = new Result<int>();
        
        int resultValue = result.InvokeResult(() => DoThingIResult());
        
        resultValue.ShouldBe(TEST_VALUE);
    }

    [Fact]
    public void ResultT_Do()
    {
        var result = new Result<int>();
        
        (Result r, int resultValue) = result.Do(() => DoThingIResult());
    }
    
    [Fact]
    public void Result_Do()
    {
        var result = new Result();
        
        Result r = result.Do(() => DoThingIResult());
    }

    [Fact]
    public async Task InvokeResultAsync_WithReturningTaskResultResult_ReturnsResultResult()
    {
        async Task<Result<int>> DoThingResultAsync()
        {
            return TEST_VALUE;
        }
        
        (Result result, int resultValue) = await Result.InvokeResultAsync(async () => await DoThingResultAsync());
        
        resultValue.ShouldBe(TEST_VALUE);
        result.HasErrors.ShouldBeFalse();
    }
}