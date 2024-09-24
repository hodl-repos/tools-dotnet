using System;

namespace tools_dotnet.Exceptions
{
    public class ApiCallFailedException : Exception
    {
        public int StatusCode { get; private set; }

        public string? Content { get; private set; }

        public ApiCallFailedException(string url, int statusCode, Exception inner) :
          base($"The API call to '{url}' failed with status code {statusCode}. Check inner exception for more details.", inner)
        {
            StatusCode = statusCode;
        }

        public ApiCallFailedException(string url, int statusCode, string? content) :
          base($"The API call to '{url}' failed with status code {statusCode}. Check the content for more details.")
        {
            StatusCode = statusCode;
            Content = content;
        }
    }
}