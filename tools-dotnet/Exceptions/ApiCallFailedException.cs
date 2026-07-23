using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>Reports a failed downstream API call and preserves its response details.</summary>
    public class ApiCallFailedException : Exception
    {
        /// <summary>Gets the downstream HTTP status code.</summary>
        public int StatusCode { get; private set; }

        /// <summary>Gets the downstream response content, when available.</summary>
        public string? Content { get; private set; }

        /// <summary>Initializes a new instance of <c>ApiCallFailedException</c>.</summary>
        public ApiCallFailedException(string url, int statusCode, Exception inner)
            : base(
                $"The API call to '{url}' failed with status code {statusCode}. Check inner exception for more details.",
                inner
            )
        {
            StatusCode = statusCode;
        }

        /// <summary>Initializes a new instance of <c>ApiCallFailedException</c>.</summary>
        public ApiCallFailedException(string url, int statusCode, string? content)
            : base(
                $"The API call to '{url}' failed with status code {statusCode}. Check the content for more details."
            )
        {
            StatusCode = statusCode;
            Content = content;
        }
    }
}
