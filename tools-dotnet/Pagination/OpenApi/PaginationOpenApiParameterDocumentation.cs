using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace tools_dotnet.Pagination.OpenApi
{
    internal sealed class PaginationOpenApiParameterDocumentation
    {
        public PaginationOpenApiParameterDocumentation(
            string description,
            IReadOnlyList<PaginationOpenApiExampleDescriptor> examples,
            JsonObject extension
        )
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Examples = examples ?? throw new ArgumentNullException(nameof(examples));
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
        }

        public string Description { get; }

        public IReadOnlyList<PaginationOpenApiExampleDescriptor> Examples { get; }

        public JsonObject Extension { get; }
    }

    internal sealed class PaginationOpenApiExampleDescriptor
    {
        public PaginationOpenApiExampleDescriptor(string name, string summary, string value)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(
                    "Example name cannot be null or whitespace.",
                    nameof(name)
                )
                : name;
            Summary = string.IsNullOrWhiteSpace(summary)
                ? throw new ArgumentException(
                    "Example summary cannot be null or whitespace.",
                    nameof(summary)
                )
                : summary;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name { get; }

        public string Summary { get; }

        public string Value { get; }
    }
}
