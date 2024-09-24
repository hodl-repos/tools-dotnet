using System;

namespace tools_dotnet.Exceptions
{
    public class FileStorageException : Exception
    {
        public FileStorageException()
        {
        }

        public FileStorageException(string? message) : base(message)
        {
        }

        public FileStorageException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}