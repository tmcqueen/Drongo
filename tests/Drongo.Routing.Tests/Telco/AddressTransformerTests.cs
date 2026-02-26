using Drongo.Routing.Telco;
using Shouldly;

namespace Drongo.Routing.Tests.Telco;

public class AddressTransformerTests
{
    [Theory]
    [InlineData("5551212", "NxxXXXX", "1 (212) $$$-$$$$", "1 (212) 555-1212")]
    [InlineData("5551212345", "NxxXXXXZ", "1 (212) $$$-$$$$ Z", "1 (212) 555-1212 345")]
    public void Transform_AppliesPatternAndTransform(string input, string pattern, string transform, string expected)
    {
        var transformer = new AddressTransformer();
        var result = transformer.Transform(input, pattern, transform);
        result.ShouldBe(expected);
    }
}
