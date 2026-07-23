using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>Reports that dependent data prevents an operation.</summary>
    public class DependentItemException : Exception
    {
        /// <summary>Gets or sets whether the dependency failure occurred during removal.</summary>
        public bool OnRemove { get; set; } = false;

        /// <summary>Initializes a new instance of <c>DependentItemException</c>.</summary>
        public DependentItemException(bool onRemove)
        {
            OnRemove = onRemove;
        }

        /// <summary>Initializes a new instance of <c>DependentItemException</c>.</summary>
        public DependentItemException(string message, bool onRemove)
            : base(message)
        {
            OnRemove = onRemove;
        }

        /// <summary>Initializes a new instance of <c>DependentItemException</c>.</summary>
        public DependentItemException(string message, bool onRemove, Exception innerException)
            : base(message, innerException)
        {
            OnRemove = onRemove;
        }
    }
}
