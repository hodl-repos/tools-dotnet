using System;

namespace tools_dotnet.Exceptions
{
    public class ConcurrentModificationException : Exception
    {
        public string? DbConcurrencyStamp { get; private set; }

        public string? RequestConcurrencyStamp { get; private set; }

        public ConcurrentModificationException()
        {
        }

        public ConcurrentModificationException(string dbConcurrencyStamp,
            string requestConcurrencyStamp)
            : base($"Concurrency stamps do not match. Database: {dbConcurrencyStamp}, " +
                $"Request: {requestConcurrencyStamp}")
        {
            DbConcurrencyStamp = dbConcurrencyStamp;
            RequestConcurrencyStamp = requestConcurrencyStamp;
        }

        public ConcurrentModificationException(string message) : base(message)
        {
        }

        public ConcurrentModificationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}