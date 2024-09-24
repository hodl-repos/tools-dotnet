using System;

namespace tools_dotnet.Exceptions
{
    public class NoPermissionException : Exception
    {
        public NoPermissionException()
        {
        }

        public NoPermissionException(string message) : base(message)
        {
        }
    }
}