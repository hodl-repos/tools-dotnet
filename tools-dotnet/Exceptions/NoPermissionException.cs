using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>Reports that the caller is not permitted to perform an operation.</summary>
    public class NoPermissionException : Exception
    {
        /// <summary>Initializes a new instance of <c>NoPermissionException</c>.</summary>
        public NoPermissionException() { }

        /// <summary>Initializes a new instance of <c>NoPermissionException</c>.</summary>
        public NoPermissionException(string message)
            : base(message) { }
    }
}
