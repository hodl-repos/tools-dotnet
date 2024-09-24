namespace tools_dotnet.Errors
{
    public class ApiPaymentRequiredError : GenericApiError
    {
        protected ApiPaymentRequiredError()
        { }

        public ApiPaymentRequiredError(string instance) : base(
            "Payment required",
            "Payment required for this action",
            instance,
            System.Net.HttpStatusCode.PaymentRequired)
        {
        }
    }
}