using System.Text;
using System.Text.RegularExpressions;
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
            new(null, null, null, null, null, null, null, null),
            new(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            ),
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
                "version2_value",
                "version2-value",
                "version2.value",
                "VERSION2-VALUE",
                "VERSION2_VALUE",
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
            new(
                "FOOBar",
                "foo_bar",
                "foo-bar",
                "foo.bar",
                "FOO-BAR",
                "FOO_BAR",
                "FooBar",
                "fooBar"
            ),
            new(
                "SimpleXMLParser",
                "simple_xml_parser",
                "simple-xml-parser",
                "simple.xml.parser",
                "SIMPLE-XML-PARSER",
                "SIMPLE_XML_PARSER",
                "SimpleXmlParser",
                "simpleXmlParser"
            ),
            new(
                "GL11Version",
                "gl11_version",
                "gl11-version",
                "gl11.version",
                "GL11-VERSION",
                "GL11_VERSION",
                "Gl11Version",
                "gl11Version"
            ),
            new(
                "IsoAlpha2",
                "iso_alpha2",
                "iso-alpha2",
                "iso.alpha2",
                "ISO-ALPHA2",
                "ISO_ALPHA2",
                "IsoAlpha2",
                "isoAlpha2"
            ),
            new(
                "foo2Bar",
                "foo2_bar",
                "foo2-bar",
                "foo2.bar",
                "FOO2-BAR",
                "FOO2_BAR",
                "Foo2Bar",
                "foo2Bar"
            ),
            new(
                "__LeadingUnderscoresValue",
                "leading_underscores_value",
                "leading-underscores-value",
                "leading.underscores.value",
                "LEADING-UNDERSCORES-VALUE",
                "LEADING_UNDERSCORES_VALUE",
                "LeadingUnderscoresValue",
                "leadingUnderscoresValue"
            ),
        ];

        private static readonly string?[] OldCompatibilityCases =
        [
            "fooBar",
            "PascalCase",
            "already_snake_case",
            "URL",
            "userID",
            "Version2Value",
            "GL11Version",
            "foo2Bar",
            "IsoAlpha2",
            "Test1",
            "Http2XX",
            null,
            "",
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

        [TestCaseSource(nameof(OldCompatibilityCases))]
        public void ToSnakeCase_ShouldMatchOldSnakeCase_ForDigitAndCompatibilityCases(string? input)
        {
            input.ToSnakeCase().ShouldBe(ToOldSnakeCase(input));
        }

        private static string? ToOldSnakeCase(string? input, char replaceChar = '_')
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var startUnderscores = Regex.Match(input, @"^_+");
            return startUnderscores
                + Regex
                    .Replace(input, @"([a-z0-9])([A-Z])", "$1" + replaceChar + "$2")
                    .ToLowerInvariant();
        }
    }
}

