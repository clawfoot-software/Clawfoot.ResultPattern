using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

public class ResultDeconstructTests
{
    [Fact]
    public async Task WithAsyncCall_DeconstructedInIf_ShouldDeconstruct()
    {
        async Task<Result> GetErrorResult()
        {
            await Task.Delay(1);
            return Result.Error("Error");
        }

        if ((await GetErrorResult() is (false, Result result)))
        {
            result.Success.ShouldBeFalse();
        }
        else
        {
            throw new Exception();
        }
    }
}