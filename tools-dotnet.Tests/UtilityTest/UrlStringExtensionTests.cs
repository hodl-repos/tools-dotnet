using FluentAssertions;
using tools_dotnet.Utility;

namespace tools_dotnet.Tests.UtilityTest
{
    [TestFixture]
    public class UrlStringExtensionTests
    {
        [TestCase("mauracher.cc", "mauracher.cc")]
        [TestCase("facebok.co.uk", "facebok.co.uk")]
        [TestCase("www.facebok.co.uk", "www.facebok.co.uk")]
        public void TestSimpleDnsParse(string input, string expectedDomain)
        {
            var result = UrlStringExtensions.ExtractDomain(input);

            result.Should().Be(expectedDomain);
        }

        [TestCase("mauracher.cc((fj")]
        public void TestFailingDnsParse(string input)
        {
            var result = UrlStringExtensions.ExtractDomain(input);

            result.Should().BeNull();
        }

        [TestCase("//shopify.com/cdn/shop/files/sterling-silber.jpg?v=1729878050&amp;width=1440", "https://shopify.com/cdn/shop/files/sterling-silber.jpg")]
        public void TestRemoveQueryParams(string input, string output)
        {
            var fixedUrl = UrlStringExtensions.SanitizeWebUrl(input);

            var result = UrlStringExtensions.RemoveQueryParams(fixedUrl);

            result.Should().Be(output);
        }
    }
}