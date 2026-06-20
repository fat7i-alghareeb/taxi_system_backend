using Taxi.Domain.Common.Results;

using Xunit;

namespace Taxi.Domain.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void SuccessOrNull_WhenValueIsNull_CreatesSuccessfulNullableResult()
    {
        var result = Result<string?>.SuccessOrNull(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Match_WhenSuccess_UsesValueBranch()
    {
        Result<int> result = 5;

        var output = result.Match(value => value + 1, _ => -1);

        Assert.Equal(6, output);
    }

    [Fact]
    public void Match_WhenError_UsesErrorBranch()
    {
        Result<int> result = Error.Validation("Test", "Bad");

        var output = result.Match(_ => 1, errors => errors.Count);

        Assert.Equal(1, output);
    }
}
