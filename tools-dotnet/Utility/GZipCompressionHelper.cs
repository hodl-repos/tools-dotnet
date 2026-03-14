using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace tools_dotnet.Utility
{
    public static class GZipCompressionHelper
    {
        public static async Task<byte[]> CompressAsync(this byte[] bytes)
        {
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.SmallestSize))
            {
                await gzipStream.WriteAsync(bytes, 0, bytes.Length);
            }

            return memoryStream.ToArray();
        }

        public static async Task<byte[]> CompressAsync(this string payload)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);

            return await CompressAsync(bytes);
        }

        public static async Task<byte[]> DecompressAsync(this byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);
            using var outputStream = new MemoryStream();
            using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                await decompressStream.CopyToAsync(outputStream);
            }

            return outputStream.ToArray();
        }

        public static async Task<string> DecompressToStringAsync(this byte[] bytes)
        {
            var decompressedBytes = await DecompressAsync(bytes);
            return Encoding.UTF8.GetString(decompressedBytes);
        }

        public static async Task<string> CompressStringToBase64Async(this string payload)
        {
            var byteContent = Encoding.UTF8.GetBytes(payload);

            return Convert.ToBase64String(await byteContent.CompressAsync());
        }

        public static async Task<string> DecompressStringFromBase64Async(this string base64String)
        {
            var byteContent = await Convert.FromBase64String(base64String).DecompressAsync();

            return Encoding.UTF8.GetString(byteContent);
        }
    }
}
