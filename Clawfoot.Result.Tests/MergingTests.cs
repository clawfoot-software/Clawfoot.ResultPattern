using Shouldly;
using FakeItEasy;

namespace Clawfoot.ResultPattern.Tests;

public class MergingTests
{
    private const string ERROR_MESSAGE = "Error";
    private const int TEST_VALUE = 5;

    [Fact]
    public void Combine_ResultT_With_Result_ReturnsNewResultWithCombinedErrors()
    {
        Result<int> result = new Result<int>(TEST_VALUE);
        Result resultObj = Result.Error(ERROR_MESSAGE);

        Result<int> merged = Result.Combine<int>(resultObj, result);

        merged.ShouldNotBe(result);
        result.Success.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        merged.Success.ShouldBeFalse();
        merged.Value.ShouldBe(TEST_VALUE);
        merged.Errors.Count().ShouldBe(1);
        merged.Errors.First().Message.ShouldBe(ERROR_MESSAGE);
    }

    [Fact]
    public void Combine_ResultT_With_ResultT_ReturnsNewResultWithCombinedErrors()
    {
        Result<int> result = new Result<int>(TEST_VALUE);
        Result<int> result2 = Result.Error<int>(ERROR_MESSAGE);

        Result<int> merged = Result.Combine(result, result2);

        merged.ShouldNotBe(result);
        result.Success.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        merged.Success.ShouldBeFalse();
        merged.Value.ShouldBe(TEST_VALUE);
        merged.Errors.Count().ShouldBe(1);
        merged.Errors.First().Message.ShouldBe(ERROR_MESSAGE);
    }

    [Fact]
    public void Combine_Result_With_ResultT_ReturnsNewResultWithCombinedErrors()
    {
        Result result = Result.Error(ERROR_MESSAGE);
        Result<int> resultObj = new Result<int>();

        Result merged = Result.Combine(new ResultBase[] { result, resultObj });

        merged.ShouldNotBe(result);
        result.Success.ShouldBeFalse();
        merged.Success.ShouldBeFalse();
        merged.Errors.Count().ShouldBe(1);
        merged.Errors.First().Message.ShouldBe(ERROR_MESSAGE);
    }

    [Fact]
    public void Combine_ResultBaseArray_CombinesWithoutCallingAsT()
    {
        Result<int> result = new Result<int>();
        Result result2 = A.Fake<Result>();

        Result merged = Result.Combine(new ResultBase[] { result, result2 });

        A.CallTo(() => result2.As<int>()).MustNotHaveHappened();
    }
}
