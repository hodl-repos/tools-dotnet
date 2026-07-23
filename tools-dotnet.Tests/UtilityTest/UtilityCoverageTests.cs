using System.Text;
using Microsoft.AspNetCore.Http;
using Shouldly;
using tools_dotnet.Utility;

namespace tools_dotnet.Tests.UtilityTest
{
    [TestFixture]
    public class UtilityCoverageTests
    {
        [Test]
        public async Task AsyncQueue_ShouldReturnItemsInFifoOrder()
        {
            var queue = new AsyncQueue<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);

            await using var enumerator = queue.GetAsyncEnumerator();

            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            enumerator.Current.ShouldBe(2);
        }

        [Test]
        public async Task AsyncQueue_GetAllAsync_ShouldExposeQueuedItems()
        {
            var queue = new AsyncQueue<string>();
            queue.Enqueue("first");

            var items = await queue.GetAllAsync();
            await using var enumerator = items.GetAsyncEnumerator();

            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            enumerator.Current.ShouldBe("first");
        }

        [Test]
        public void EmailExtensions_ShouldExtractAndNormalizeAddresses()
        {
            var addresses = EmailStringExtensions.ExtractEmailAdresses(
                "Alice <alice@example.com>",
                "bob@example.com",
                "alice@example.com",
                null
            );

            addresses.ShouldBe(["alice@example.com", "bob@example.com"]);
            "Carol <carol@example.com>".TryExtractEmail().ShouldBe("carol@example.com");
            "not an email".TryExtractEmail().ShouldBeNull();
            string.Empty.ExtractEmailAdresses().ShouldBeEmpty();
        }

        [Test]
        public async Task GZipExtensions_ShouldRoundTripBytesAndText()
        {
            var bytes = Encoding.UTF8.GetBytes("Grüße from tools-dotnet");

            var compressedBytes = await bytes.CompressAsync();
            var decompressedBytes = await compressedBytes.DecompressAsync();
            var compressedText = await "another payload".CompressStringToBase64Async();

            decompressedBytes.ShouldBe(bytes);
            (await compressedBytes.DecompressToStringAsync()).ShouldBe("Grüße from tools-dotnet");
            (await compressedText.DecompressStringFromBase64Async()).ShouldBe("another payload");
        }

        [Test]
        public void ParseValues_ShouldSupportEveryFailureBehavior()
        {
            ParseExtensions.ParseValues<int>(new[] { "1", "2" }).ShouldBe([1, 2]);
            ParseExtensions
                .ParseValues<int>(
                    new[] { "1", "bad", "2" },
                    ParseExtensions.ParseFailureBehavior.IgnoreValue
                )
                .ShouldBe([1, 2]);
            ParseExtensions
                .ParseValues<int>(
                    new[] { "1", "bad" },
                    ParseExtensions.ParseFailureBehavior.ReturnNull
                )
                .ShouldBeNull();
            ParseExtensions
                .ParseValues<int>(
                    new[] { "1", "bad" },
                    ParseExtensions.ParseFailureBehavior.ReturnEmpty
                )
                .ShouldBeEmpty();

            Should.Throw<FormatException>(() =>
                ParseExtensions.ParseValues<int>(
                    new[] { "bad" },
                    ParseExtensions.ParseFailureBehavior.Throw
                )
            );
        }

        [Test]
        public async Task StreamResultAsync_ShouldWriteCamelCaseNdjson()
        {
            var context = new DefaultHttpContext();
            await using var body = new MemoryStream();
            context.Response.Body = body;

            await context.StreamResultAsync(
                context.Response,
                GetStreamItems()
            );

            context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
            context.Response.ContentType.ShouldBe("application/x-ndjson");

            body.Position = 0;
            using var reader = new StreamReader(body);
            (await reader.ReadToEndAsync()).ShouldBe(
                "{\"itemId\":1,\"displayName\":\"first\"}\n"
                    + "{\"itemId\":2,\"displayName\":\"second\"}\n"
            );
        }

        [Test]
        public void StringExtensions_ShouldHandleJoinCleanupAndProximity()
        {
            StringExtensions.StringJoinWhereNotEmpty(", ", " alpha ", null, "", "beta")
                .ShouldBe(" alpha , beta");
            new[] { "alpha", "", " ", "beta" }.ArrayRemoveEmptyParts()
                .ShouldBe(["alpha", "beta"]);

            StringExtensions.CalculateStringProximity("same", "same").ShouldBe(1);
            StringExtensions.CalculateStringProximity("", "value").ShouldBe(0);
            StringExtensions.CalculateStringProximity("MARTHA", "MARHTA").ShouldBeGreaterThan(0.9);
            StringExtensions.CalculateStringProximity("abc", "xyz").ShouldBe(0);
        }

        private static async IAsyncEnumerable<StreamItem> GetStreamItems()
        {
            yield return new StreamItem(1, "first");
            await Task.Yield();
            yield return new StreamItem(2, "second");
        }

        private sealed record StreamItem(int ItemId, string DisplayName);
    }
}
