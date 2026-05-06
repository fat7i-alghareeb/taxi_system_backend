using Taxi.Domain.Common;

using Xunit;

namespace Taxi.Domain.UnitTests.Common;

public class LocalizedTextTests
{
    [Theory]
    [InlineData("en", "Hello")]
    [InlineData("ar", "Marhaba")]
    [InlineData("nl", "Hallo")]
    [InlineData("fr", "Hello")]
    public void GetTranslation_ReturnsExpected(string languageCode, string expected)
    {
        var text = new LocalizedText("Hello", "Marhaba", "Hallo");

        var actual = text.GetTranslation(languageCode);

        Assert.Equal(expected, actual);
    }
}
