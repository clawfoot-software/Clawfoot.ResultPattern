using Shouldly;
using FakeItEasy;

namespace Clawfoot.Result.Tests;

public class MergingTests
{
    private const string ERROR_MESSAGE = "Error";
    private const int TEST_VALUE = 5;
    private const int TEST_VALUE2 = 55;
    
    [Fact]
    public void ResultT_WithResult_Merges()
    {
        Result<int> result = new Result<int>(TEST_VALUE);
        Result resultObj = new Result();
        
        resultObj.AddError(ERROR_MESSAGE);
        
        Result<int>? merged = result.MergeResults(resultObj);
        
        merged.ShouldBe(result);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBe(TEST_VALUE);
        result.Errors.Count().ShouldBe(1);
        result.Errors.First().Message.ShouldBe(ERROR_MESSAGE);
    }
    
    [Fact]
    public void ResultT_WithResultT_Merges()
    {
        Result<int> result = new Result<int>(TEST_VALUE);
        Result<int> result2 = new Result<int>();
        
        result2.AddError(ERROR_MESSAGE);
        
        Result<int>? merged = result.MergeResults(result2);
        
        merged.ShouldBe(result);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBe(TEST_VALUE);
        result.Errors.Count().ShouldBe(1);
        result.Errors.First().Message.ShouldBe(ERROR_MESSAGE);
    }
    
    [Fact]
    public void Result_WithResultT_Merges()
    {
        Result result = new Result();
        Result<int> resultObj = new Result<int>();
        
        result.AddError(ERROR_MESSAGE);
        
        Result merged = result.MergeResults(resultObj);
        
        merged.ShouldBe(result);
        result.Success.ShouldBeFalse();
        result.Errors.Count().ShouldBe(1);
        result.Errors.First().Message.ShouldBe(ERROR_MESSAGE);
    }

    [Fact]
    public void Regression_GenericResult_MergeInNonGeneric_ShouldNeverCallAsT()
    {
        // Regression test to ensure that we are avoiding implicit casts instead of direct implementation calls
        // The implicit Result -> Result<T> cast calls .As<T>() and was causing a stack overflow
        
        Result<int> result = new Result<int>();
        Result result2 = A.Fake<Result>();
        
        result.MergeResults(result2);
        
        A.CallTo(() => result2.As<int>()).MustNotHaveHappened();
    }
}