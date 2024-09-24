using System;

namespace tools_dotnet.Exceptions
{
    public class PaymentRequiredException : Exception
    {
        public PaymentRequiredException()
        {
        }

        public PaymentRequiredException(string message) : base(message)
        {
        }
    }
}