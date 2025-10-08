using System.Text.Json;
using System.Text.Json.Serialization;

namespace tools_dotnet.Utility
{
    public class SnakeCaseEnumConverter<T> : JsonStringEnumConverter where T : struct
    {
        public SnakeCaseEnumConverter() : base(new SnakeCaseNamingPolicy())
        {
        }
    }

    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return name.ToSnakeCase()!;
        }
    }
}