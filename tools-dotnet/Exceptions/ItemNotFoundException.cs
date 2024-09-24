using System;

namespace tools_dotnet.Exceptions
{
    public class ItemNotFoundException : Exception
    {
        public ItemNotFoundException(string message) : base(message)
        {
        }

        public ItemNotFoundException(string name, int key)
            : this(name, key.ToString())
        {
        }

        public ItemNotFoundException(string name, string key) : base($"Could not find '{name}' with key '{key}'")
        {
        }

        public static ItemNotFoundException Create<TKeyType>(string entityName, TKeyType key) where TKeyType : struct
        {
            return new ItemNotFoundException(entityName, key.ToString() ?? "");
        }

        public static ItemNotFoundException Create<TKeyType>(string entityName, TKeyType[] key)
        {
            return new ItemNotFoundException(entityName, key.ToString() ?? "");
        }

        public static ItemNotFoundException Create(string entityName, string? key)
        {
            return new ItemNotFoundException(entityName, key?.ToString() ?? "");
        }
    }
}