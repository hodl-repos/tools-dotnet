using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace tools_dotnet.Utility
{
    public static class ResultStreamExtensions
    {
        /// <summary>
        /// wraps items one-by-one in ndjson, returns everything needed to http-reponse
        /// </summary>
        public static async Task StreamResultAsync<T>(HttpContext httpContext, HttpResponse response, IAsyncEnumerable<T> enumerable, JsonSerializerOptions? options = null)
        {
            if (options == null)
            {
                options = new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
            }

            response.ContentType = "application/x-ndjson";
            response.StatusCode = (int)HttpStatusCode.OK;

            // Disable response buffering to stream data directly to the client
            httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            await response.Body.FlushAsync();

            await foreach (var item in enumerable)
            {
                // Serialize the item to JSON
                var json = JsonSerializer.Serialize(item, options);

                // Write the JSON followed by a newline character
                await response.WriteAsync(json + "\n");

                // Flush the response to send data to the client immediately
                await response.Body.FlushAsync();
            }

            response.Body.Close();
        }
    }
}