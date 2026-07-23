using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>Reports that an item changed since the caller last read it.</summary>
    public class ConcurrentModificationException : Exception
    {
        /// <summary>Gets the current concurrency token stored by the database.</summary>
        public string? DbConcurrencyStamp { get; private set; }

        /// <summary>Gets the stale concurrency token supplied by the request.</summary>
        public string? RequestConcurrencyStamp { get; private set; }

        /// <summary>Initializes a new instance of <c>ConcurrentModificationException</c>.</summary>
        public ConcurrentModificationException() { }

        /// <summary>Initializes a new instance of <c>ConcurrentModificationException</c>.</summary>
        public ConcurrentModificationException(
            string? dbConcurrencyStamp,
            string? requestConcurrencyStamp
        )
            : base(CreateMessage(dbConcurrencyStamp, requestConcurrencyStamp))
        {
            DbConcurrencyStamp = dbConcurrencyStamp;
            RequestConcurrencyStamp = requestConcurrencyStamp;
        }

        /// <summary>Initializes a new instance of <c>ConcurrentModificationException</c>.</summary>
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

        /// <summary>Initializes a new instance of <c>ConcurrentModificationException</c>.</summary>
        public ConcurrentModificationException(string message)
            : base(message) { }

        /// <summary>Initializes a new instance of <c>ConcurrentModificationException</c>.</summary>
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
