using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>Reports a file storage operation failure.</summary>
    public class FileStorageException : Exception
    {
        /// <summary>Initializes a new instance of <c>FileStorageException</c>.</summary>
        public FileStorageException() { }

        /// <summary>Initializes a new instance of <c>FileStorageException</c>.</summary>
        public FileStorageException(string? message)
            : base(message) { }

        /// <summary>Initializes a new instance of <c>FileStorageException</c>.</summary>
        public FileStorageException(string? message, Exception? innerException)
            : base(message, innerException) { }
    }
}
