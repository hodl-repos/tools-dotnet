using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>Reports that an item conflicts with existing data.</summary>
    public class ConflictingItemException : Exception
    {
        /// <summary>Initializes a new instance of <c>ConflictingItemException</c>.</summary>
        public ConflictingItemException() { }

        /// <summary>Initializes a new instance of <c>ConflictingItemException</c>.</summary>
        public ConflictingItemException(string message)
            : base(message) { }

        /// <summary>Initializes a new instance of <c>ConflictingItemException</c>.</summary>
        public ConflictingItemException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
