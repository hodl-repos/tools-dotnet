using System;

namespace tools_dotnet.Exceptions
{
    public class DependentItemException : Exception
    {
        public bool OnRemove { get; set; } = false;

        public DependentItemException(bool onRemove)
        {
            OnRemove = onRemove;
        }

        public DependentItemException(string message, bool onRemove) : base(message)
        {
            OnRemove = onRemove;
        }

        public DependentItemException(string message, bool onRemove, Exception innerException) : base(message, innerException)
        {
            OnRemove = onRemove;
        }
    }
}