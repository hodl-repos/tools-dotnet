using Shouldly;
using tools_dotnet.Utility;

namespace tools_dotnet.Tests.UtilityTest
{
    [TestFixture]
    public class StringCaseExtensionsTests
    {
        public sealed record ConversionExpectation(
            string? Input,
            string SnakeCase,
            string KebabCase,
            string DotCase,
            string CobolCase,
            string ScreamingSnakeCase,
            string PascalCase,
            string CamelCase
        );

        private static readonly ConversionExpectation[] Cases =
        [
            new(null, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
            new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
            new(
                "XMLHttpRequest",
                "xml_http_request",
                "xml-http-request",
                "xml.http.request",
                "XML-HTTP-REQUEST",
                "XML_HTTP_REQUEST",
                "XmlHttpRequest",
                "xmlHttpRequest"
            ),
            new(
                "Version2Value",
                "version_2_value",
                "version-2-value",
                "version.2.value",
                "VERSION-2-VALUE",
                "VERSION_2_VALUE",
                "Version2Value",
                "version2Value"
            ),
            new(
                " user_name-value.test/path\\segment ",
                "user_name_value_test_path_segment",
                "user-name-value-test-path-segment",
                "user.name.value.test.path.segment",
                "USER-NAME-VALUE-TEST-PATH-SEGMENT",
                "USER_NAME_VALUE_TEST_PATH_SEGMENT",
                "UserNameValueTestPathSegment",
                "userNameValueTestPathSegment"
            ),
        ];

        [TestCaseSource(nameof(Cases))]
        public void Converts_AllPublicCases(ConversionExpectation expectation)
        {
            expectation.Input.ToSnakeCase().ShouldBe(expectation.SnakeCase);
            expectation.Input.ToKebabCase().ShouldBe(expectation.KebabCase);
            expectation.Input.ToDotCase().ShouldBe(expectation.DotCase);
            expectation.Input.ToCobolCase().ShouldBe(expectation.CobolCase);
            expectation.Input.ToScreamingSnakeCase().ShouldBe(expectation.ScreamingSnakeCase);
            expectation.Input.ToPascalCase().ShouldBe(expectation.PascalCase);
            expectation.Input.ToCamelCase().ShouldBe(expectation.CamelCase);
        }
    }
}
