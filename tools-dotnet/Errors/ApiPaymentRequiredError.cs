namespace tools_dotnet.Errors
{
    /// <summary>Represents an API error that requires payment.</summary>
    public class ApiPaymentRequiredError : GenericApiError
    {
        /// <summary>Initializes a new instance of <c>ApiPaymentRequiredError</c>.</summary>
        protected ApiPaymentRequiredError() { }

        /// <summary>Initializes a new instance of <c>ApiPaymentRequiredError</c>.</summary>
        public ApiPaymentRequiredError(string instance)
            : base(
                "Payment required",
                "Payment required for this action",
                instance,
                System.Net.HttpStatusCode.PaymentRequired
            )
        { }
    }
}
