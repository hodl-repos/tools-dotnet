using System.Text.Json;
using Shouldly;
using tools_dotnet.Utility;

namespace tools_dotnet.Tests.UtilityTest
{
    [TestFixture]
    public class EnumCaseConverterTests
    {
        private enum SampleEnum
        {
            XMLHttpRequest,
            HttpStatusOk,
        }

        [Test]
        public void Serialize_UsesSnakeCase()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new EnumCaseConverter(StringCaseType.SnakeCase));

            string json = JsonSerializer.Serialize(SampleEnum.XMLHttpRequest, options);

            json.ShouldBe("\"xml_http_request\"");
        }

        [Test]
        public void Serialize_UsesUpperKebabCase()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new EnumCaseConverter(StringCaseType.UpperKebabCase));

            string json = JsonSerializer.Serialize(SampleEnum.HttpStatusOk, options);

            json.ShouldBe("\"HTTP-STATUS-OK\"");
        }

        [Test]
        public void Deserialize_UsesConfiguredCase()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new EnumCaseConverter(StringCaseType.SnakeCase));

            SampleEnum value = JsonSerializer.Deserialize<SampleEnum>("\"http_status_ok\"", options);

            value.ShouldBe(SampleEnum.HttpStatusOk);
        }
    }
}
