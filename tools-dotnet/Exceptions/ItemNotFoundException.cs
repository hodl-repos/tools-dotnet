using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>Reports that an entity with the requested key was not found.</summary>
    public class ItemNotFoundException : Exception
    {
        /// <summary>Initializes a new instance of <c>ItemNotFoundException</c>.</summary>
        public ItemNotFoundException(string message)
            : base(message) { }

        /// <summary>Initializes a new instance of <c>ItemNotFoundException</c>.</summary>
        public ItemNotFoundException(string name, int key)
            : this(name, key.ToString()) { }

        /// <summary>Initializes a new instance of <c>ItemNotFoundException</c>.</summary>
        public ItemNotFoundException(string name, string key)
            : base($"Could not find '{name}' with key '{key}'") { }

        /// <summary>Creates an exception for an entity and value-type key.</summary>
        public static ItemNotFoundException Create<TKeyType>(string entityName, TKeyType key)
            where TKeyType : struct
        {
            return new ItemNotFoundException(entityName, key.ToString() ?? "");
        }

        /// <summary>Creates an item-not-found exception for an entity and key.</summary>
        public static ItemNotFoundException Create<TKeyType>(string entityName, TKeyType[] key)
        {
            return new ItemNotFoundException(entityName, key.ToString() ?? "");
        }

        /// <summary>Creates an item-not-found exception for an entity and key.</summary>
        public static ItemNotFoundException Create(string entityName, string? key)
        {
            return new ItemNotFoundException(entityName, key?.ToString() ?? "");
        }
    }
}
