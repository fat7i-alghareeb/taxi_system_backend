using Taxi.Domain.Common;

using Xunit;

namespace Taxi.Domain.UnitTests.Common;

public class LocalizedTextTests
{
    [Theory]
    [InlineData("en", "Hello")]
    [InlineData("ar", "Marhaba")]
    [InlineData("de", "Hallo De")]
    [InlineData("pl", "Czesc")]
    [InlineData("uk", "Pryvit")]
    [InlineData("fr", "Bonjour")]
    [InlineData("es", "Hola")]
    [InlineData("ro", "Salut")]
    public void GetTranslation_ReturnsExpected(string languageCode, string expected)
    {
        var text = new LocalizedText("Hello", "Marhaba", "Hallo", "Hallo De", "Czesc", "Pryvit", "Bonjour", "Hola", "Salut");

        var actual = text.GetTranslation(languageCode);

        Assert.Equal(expected, actual);
    }
}
