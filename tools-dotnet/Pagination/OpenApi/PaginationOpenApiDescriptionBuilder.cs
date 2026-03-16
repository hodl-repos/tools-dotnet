using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace tools_dotnet.Pagination.OpenApi
{
    internal static class PaginationOpenApiDescriptionBuilder
    {
        private const string PaginationExtensionName = "x-tools-dotnet-pagination";

        public static string ExtensionName => PaginationExtensionName;

        public static PaginationOpenApiParameterDocumentation BuildFiltersDocumentation(
            IReadOnlyList<PaginationOpenApiFieldDescriptor> fields
        )
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var filterableFields = fields.Where(x => x.CanFilter).ToArray();

            if (filterableFields.Length == 0)
            {
                return new PaginationOpenApiParameterDocumentation(
                    string.Empty,
                    Array.Empty<PaginationOpenApiExampleDescriptor>(),
                    new JsonObject()
                );
            }

            var examples = BuildFilterExamples(filterableFields);
            var description = string.Join(
                Environment.NewLine,
                [
                    "Syntax: field{operator}value. Use ',' for AND and '|' for OR.",
                    "Escape special characters with '\\'. Use 'null' for null.",
                    $"Examples: {FormatExamples(examples)}",
                    $"Allowed fields: {string.Join(", ", filterableFields.Select(x => $"`{x.Name}`"))}",
                    "See `x-tools-dotnet-pagination` for structured types and operators.",
                ]
            );

            var extension = new JsonObject
            {
                ["mode"] = "filters",
                ["syntax"] = new JsonObject
                {
                    ["expression"] = "field{operator}value",
                    ["andSeparator"] = ",",
                    ["orSeparator"] = "|",
                    ["escapeCharacter"] = "\\",
                    ["nullLiteral"] = "null",
                },
                ["examples"] = BuildExampleArray(examples),
                ["fields"] = BuildFilterFieldArray(filterableFields),
            };

            return new PaginationOpenApiParameterDocumentation(description, examples, extension);
        }

        public static PaginationOpenApiParameterDocumentation BuildSortsDocumentation(
            IReadOnlyList<PaginationOpenApiFieldDescriptor> fields
        )
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var sortableFields = fields.Where(x => x.CanSort).ToArray();

            if (sortableFields.Length == 0)
            {
                return new PaginationOpenApiParameterDocumentation(
                    string.Empty,
                    Array.Empty<PaginationOpenApiExampleDescriptor>(),
                    new JsonObject()
                );
            }

            var examples = BuildSortExamples(sortableFields);
            var description = string.Join(
                Environment.NewLine,
                [
                    "Syntax: field for ascending, -field for descending, comma for multiple sorts.",
                    $"Examples: {FormatExamples(examples)}",
                    $"Allowed fields: {string.Join(", ", sortableFields.Select(x => $"`{x.Name}`"))}",
                    "See `x-tools-dotnet-pagination` for structured field metadata.",
                ]
            );

            var extension = new JsonObject
            {
                ["mode"] = "sorts",
                ["syntax"] = new JsonObject
                {
                    ["ascending"] = "field",
                    ["descendingPrefix"] = "-",
                    ["separator"] = ",",
                },
                ["examples"] = BuildExampleArray(examples),
                ["fields"] = BuildSortFieldArray(sortableFields),
            };

            return new PaginationOpenApiParameterDocumentation(description, examples, extension);
        }

        private static JsonArray BuildFilterFieldArray(
            IReadOnlyList<PaginationOpenApiFieldDescriptor> fields
        )
        {
            var array = new JsonArray();

            foreach (var field in fields)
            {
                var fieldObject = BuildBaseFieldObject(field);
                fieldObject["operators"] = new JsonArray(
                    field.Operators.Select(x => (JsonNode?)x.Id).ToArray()
                );
                array.Add(fieldObject);
            }

            return array;
        }

        private static JsonArray BuildSortFieldArray(
            IReadOnlyList<PaginationOpenApiFieldDescriptor> fields
        )
        {
            var array = new JsonArray();

            foreach (var field in fields)
            {
                array.Add(BuildBaseFieldObject(field));
            }

            return array;
        }

        private static JsonObject BuildBaseFieldObject(PaginationOpenApiFieldDescriptor field)
        {
            return new JsonObject
            {
                ["name"] = field.Name,
                ["type"] = field.FilterTypeDisplayNameOverride
                    ?? PaginationOpenApiMetadataProvider.GetTypeDisplayName(field.MemberType),
                ["source"] = field.Source,
            };
        }

        private static JsonArray BuildExampleArray(
            IReadOnlyList<PaginationOpenApiExampleDescriptor> examples
        )
        {
            return new JsonArray(examples.Select(x => (JsonNode?)x.Value).ToArray());
        }

        private static string FormatExamples(
            IReadOnlyList<PaginationOpenApiExampleDescriptor> examples
        )
        {
            return string.Join(", ", examples.Select(x => $"`{x.Value}`"));
        }

        private static IReadOnlyList<PaginationOpenApiExampleDescriptor> BuildFilterExamples(
            IReadOnlyList<PaginationOpenApiFieldDescriptor> fields
        )
        {
            var result = new List<PaginationOpenApiExampleDescriptor>();

            var primaryField = fields.FirstOrDefault(IsStringField) ?? fields[0];
            AddUniqueExample(
                result,
                "simple",
                "Simple equality filter",
                BuildFilterExample(primaryField, preferComparison: false)
            );

            var comparableField = fields.FirstOrDefault(IsComparableField);

            if (comparableField != null)
            {
                AddUniqueExample(
                    result,
                    "comparison",
                    "Comparison filter",
                    BuildFilterExample(comparableField, preferComparison: true)
                );
            }

            var secondaryField =
                fields.FirstOrDefault(x => !ReferenceEquals(x, primaryField) && IsBooleanField(x))
                ?? fields.FirstOrDefault(x => !ReferenceEquals(x, primaryField));

            if (secondaryField != null)
            {
                var combined = string.Join(
                    ",",
                    new[]
                    {
                        BuildFilterExample(primaryField, preferComparison: false),
                        BuildFilterExample(secondaryField, preferComparison: false),
                    }
                );

                AddUniqueExample(result, "combined", "Combined AND filter", combined);
            }

            return result;
        }

        private static IReadOnlyList<PaginationOpenApiExampleDescriptor> BuildSortExamples(
            IReadOnlyList<PaginationOpenApiFieldDescriptor> fields
        )
        {
            var result = new List<PaginationOpenApiExampleDescriptor>();
            var primaryField = fields.FirstOrDefault(IsStringField) ?? fields[0];
            var descendingField =
                fields.FirstOrDefault(x => IsDateOrTimeField(x) && !ReferenceEquals(x, primaryField))
                ?? fields.FirstOrDefault(x => !ReferenceEquals(x, primaryField))
                ?? primaryField;

            AddUniqueExample(result, "ascending", "Ascending sort", primaryField.Name);
            AddUniqueExample(result, "descending", "Descending sort", $"-{descendingField.Name}");

            var multiplePrimary =
                fields.FirstOrDefault(x => IsEnumField(x) && !ReferenceEquals(x, descendingField))
                ?? primaryField;
            var multipleSecondary =
                !ReferenceEquals(descendingField, multiplePrimary)
                    ? descendingField
                    : fields.FirstOrDefault(x => !ReferenceEquals(x, multiplePrimary));

            if (multipleSecondary != null)
            {
                AddUniqueExample(
                    result,
                    "multiple",
                    "Multiple sorts",
                    $"{multiplePrimary.Name},-{multipleSecondary.Name}"
                );
            }

            return result;
        }

        private static void AddUniqueExample(
            ICollection<PaginationOpenApiExampleDescriptor> examples,
            string name,
            string summary,
            string value
        )
        {
            if (examples.Any(x => string.Equals(x.Value, value, StringComparison.Ordinal)))
            {
                return;
            }

            examples.Add(new PaginationOpenApiExampleDescriptor(name, summary, value));
        }

        private static bool IsComparableField(PaginationOpenApiFieldDescriptor field)
        {
            return field.Operators.Any(x =>
                string.Equals(x.Id, ">", StringComparison.Ordinal)
                || string.Equals(x.Id, ">=", StringComparison.Ordinal)
                || string.Equals(x.Id, "<", StringComparison.Ordinal)
                || string.Equals(x.Id, "<=", StringComparison.Ordinal)
            );
        }

        private static bool IsStringField(PaginationOpenApiFieldDescriptor field)
        {
            return (Nullable.GetUnderlyingType(field.MemberType) ?? field.MemberType) == typeof(string);
        }

        private static bool IsBooleanField(PaginationOpenApiFieldDescriptor field)
        {
            return (Nullable.GetUnderlyingType(field.MemberType) ?? field.MemberType) == typeof(bool);
        }

        private static bool IsEnumField(PaginationOpenApiFieldDescriptor field)
        {
            return (Nullable.GetUnderlyingType(field.MemberType) ?? field.MemberType).IsEnum;
        }

        private static bool IsDateOrTimeField(PaginationOpenApiFieldDescriptor field)
        {
            var underlyingType = Nullable.GetUnderlyingType(field.MemberType) ?? field.MemberType;

            return underlyingType == typeof(DateTime)
                || underlyingType == typeof(DateTimeOffset)
                || underlyingType == typeof(DateOnly)
                || underlyingType == typeof(TimeOnly)
                || underlyingType == typeof(TimeSpan);
        }

        private static string BuildFilterExample(
            PaginationOpenApiFieldDescriptor field,
            bool preferComparison
        )
        {
            if (field.Source == "custom")
            {
                return $"{field.Name}==value";
            }

            var value = GetExampleValue(field.MemberType);
            var operatorId =
                preferComparison
                && field.Operators.Any(x => string.Equals(x.Id, ">=", StringComparison.Ordinal))
                    ? ">="
                    : field.Operators.Any(x => string.Equals(x.Id, "==", StringComparison.Ordinal))
                        ? "=="
                        : field.Operators.FirstOrDefault()?.Id ?? "==";

            return $"{field.Name}{operatorId}{value}";
        }

        private static string GetExampleValue(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(string))
            {
                return "sample";
            }

            if (underlyingType == typeof(bool))
            {
                return "true";
            }

            if (underlyingType == typeof(Guid))
            {
                return "00000000-0000-0000-0000-000000000001";
            }

            if (underlyingType.IsEnum)
            {
                return System.Enum.GetNames(underlyingType).FirstOrDefault() ?? "value";
            }

            if (
                underlyingType == typeof(DateTime)
                || underlyingType == typeof(DateTimeOffset)
                || underlyingType == typeof(DateOnly)
            )
            {
                return "2024-01-01";
            }

            if (underlyingType == typeof(TimeOnly) || underlyingType == typeof(TimeSpan))
            {
                return "12:00:00";
            }

            if (
                underlyingType == typeof(byte)
                || underlyingType == typeof(sbyte)
                || underlyingType == typeof(short)
                || underlyingType == typeof(ushort)
                || underlyingType == typeof(int)
                || underlyingType == typeof(uint)
                || underlyingType == typeof(long)
                || underlyingType == typeof(ulong)
                || underlyingType == typeof(float)
                || underlyingType == typeof(double)
                || underlyingType == typeof(decimal)
            )
            {
                return "42";
            }

            return "value";
        }
    }
}
