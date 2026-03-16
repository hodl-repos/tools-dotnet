using System;

namespace tools_dotnet.Exceptions
{
    public class ConcurrentModificationException : Exception
    {
        public string? DbConcurrencyStamp { get; private set; }

        public string? RequestConcurrencyStamp { get; private set; }

        public ConcurrentModificationException() { }

        public ConcurrentModificationException(
            string? dbConcurrencyStamp,
            string? requestConcurrencyStamp
        )
            : base(CreateMessage(dbConcurrencyStamp, requestConcurrencyStamp))
        {
            DbConcurrencyStamp = dbConcurrencyStamp;
            RequestConcurrencyStamp = requestConcurrencyStamp;
        }

        public ConcurrentModificationException(
            string? dbConcurrencyStamp,
            string? requestConcurrencyStamp,
            Exception innerException
        )
            : base(CreateMessage(dbConcurrencyStamp, requestConcurrencyStamp), innerException)
        {
            DbConcurrencyStamp = dbConcurrencyStamp;
            RequestConcurrencyStamp = requestConcurrencyStamp;
        }

        public ConcurrentModificationException(string message)
            : base(message) { }

        public ConcurrentModificationException(string message, Exception innerException)
            : base(message, innerException) { }

        private static string CreateMessage(
            string? dbConcurrencyStamp,
            string? requestConcurrencyStamp
        )
        {
            return $"Concurrency stamps do not match. Database: {dbConcurrencyStamp ?? "null"}, "
                + $"Request: {requestConcurrencyStamp ?? "null"}";
        }
    }
}
