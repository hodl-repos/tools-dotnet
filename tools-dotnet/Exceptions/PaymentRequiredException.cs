using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>Reports that payment is required before an operation can continue.</summary>
    public class PaymentRequiredException : Exception
    {
        /// <summary>Initializes a new instance of <c>PaymentRequiredException</c>.</summary>
        public PaymentRequiredException() { }

        /// <summary>Initializes a new instance of <c>PaymentRequiredException</c>.</summary>
        public PaymentRequiredException(string message)
            : base(message) { }
    }
}
