namespace tools_dotnet.Enum
{
    /// <summary>Defines values used by <c>StringCaseType</c>.</summary>
    public enum StringCaseType
    {
        /// <summary>Preserves the original value.</summary>
        Original = 0,
        /// <summary>Uses camel case.</summary>
        CamelCase = 1,
        /// <summary>Uses Pascal case.</summary>
        PascalCase = 2,
        /// <summary>Uses snake case.</summary>
        SnakeCase = 3,
        /// <summary>Uses kebab case.</summary>
        KebabCase = 4,
        /// <summary>Uses uppercase kebab case.</summary>
        UpperKebabCase = 5,
        /// <summary>Alias for uppercase kebab case.</summary>
        CobolCase = UpperKebabCase,
        /// <summary>Uses uppercase snake case.</summary>
        ScreamingSnakeCase = 6,
        /// <summary>Alias for uppercase snake case.</summary>
        UpperSnakeCase = ScreamingSnakeCase,
        /// <summary>Uses dot-separated case.</summary>
        DotCase = 7,
    }
}
