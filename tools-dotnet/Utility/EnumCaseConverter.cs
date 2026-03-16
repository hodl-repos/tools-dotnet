using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tools_dotnet.Utility
{
    public sealed class EnumCaseConverter : JsonStringEnumConverter
    {
        public EnumCaseConverter(StringCaseType stringCaseType, bool allowIntegerValues = true)
            : base(EnumCaseNamingPolicy.Get(stringCaseType), allowIntegerValues) { }
    }

    internal sealed class EnumCaseNamingPolicy : JsonNamingPolicy
    {
        private static readonly EnumCaseNamingPolicy CamelCasePolicy = new(StringCaseType.CamelCase);
        private static readonly EnumCaseNamingPolicy PascalCasePolicy = new(StringCaseType.PascalCase);
        private static readonly EnumCaseNamingPolicy SnakeCasePolicy = new(StringCaseType.SnakeCase);
        private static readonly EnumCaseNamingPolicy KebabCasePolicy = new(StringCaseType.KebabCase);
        private static readonly EnumCaseNamingPolicy UpperKebabCasePolicy = new(
            StringCaseType.UpperKebabCase
        );
        private static readonly EnumCaseNamingPolicy ScreamingSnakeCasePolicy = new(
            StringCaseType.ScreamingSnakeCase
        );
        private static readonly EnumCaseNamingPolicy DotCasePolicy = new(StringCaseType.DotCase);
        private static readonly JsonNamingPolicy IdentityPolicy = new IdentityNamingPolicy();

        private readonly StringCaseType _stringCaseType;

        private EnumCaseNamingPolicy(StringCaseType stringCaseType)
        {
            _stringCaseType = stringCaseType;
        }

        public override string ConvertName(string name) =>
            _stringCaseType switch
            {
                StringCaseType.CamelCase => StringCaseExtensions.ToCamelCase(name),
                StringCaseType.PascalCase => StringCaseExtensions.ToPascalCase(name),
                StringCaseType.SnakeCase => StringCaseExtensions.ToSnakeCase(name),
                StringCaseType.KebabCase => StringCaseExtensions.ToKebabCase(name),
                StringCaseType.UpperKebabCase => StringCaseExtensions.ToCobolCase(name),
                StringCaseType.ScreamingSnakeCase => StringCaseExtensions.ToScreamingSnakeCase(name),
                StringCaseType.DotCase => StringCaseExtensions.ToDotCase(name),
                StringCaseType.Original => name,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(_stringCaseType),
                    _stringCaseType,
                    "Unsupported string case type."
                ),
            };

        internal static JsonNamingPolicy Get(StringCaseType stringCaseType) =>
            stringCaseType switch
            {
                StringCaseType.Original => IdentityPolicy,
                StringCaseType.CamelCase => CamelCasePolicy,
                StringCaseType.PascalCase => PascalCasePolicy,
                StringCaseType.SnakeCase => SnakeCasePolicy,
                StringCaseType.KebabCase => KebabCasePolicy,
                StringCaseType.UpperKebabCase => UpperKebabCasePolicy,
                StringCaseType.ScreamingSnakeCase => ScreamingSnakeCasePolicy,
                StringCaseType.DotCase => DotCasePolicy,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(stringCaseType),
                    stringCaseType,
                    "Unsupported string case type."
                ),
            };

        private sealed class IdentityNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name;
        }
    }
}
