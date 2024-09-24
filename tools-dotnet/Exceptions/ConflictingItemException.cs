using System;

namespace tools_dotnet.Exceptions
{
    public class ConflictingItemException : Exception
    {
        public ConflictingItemException()
        {
        }

        public ConflictingItemException(string message) : base(message)
        {
        }

        public ConflictingItemException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}