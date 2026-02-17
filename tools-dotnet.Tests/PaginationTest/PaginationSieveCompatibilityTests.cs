using Shouldly;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class PaginationSieveCompatibilityTests
    {
        private static readonly PaginationProcessor Processor = new();

        public static IEnumerable<TestCaseData> OperatorTokenCases()
        {
            yield return new TestCaseData("==", PaginationOperator.Equal);
            yield return new TestCaseData("==*", PaginationOperator.EqualCaseInsensitive);
            yield return new TestCaseData("!=", PaginationOperator.NotEquals);
            yield return new TestCaseData("!=*", PaginationOperator.NotEqualsCaseInsensitive);
            yield return new TestCaseData(">", PaginationOperator.GreaterThan);
            yield return new TestCaseData(">=", PaginationOperator.GreaterThanOrEqual);
            yield return new TestCaseData("<", PaginationOperator.LessThan);
            yield return new TestCaseData("<=", PaginationOperator.LessThanOrEqual);
            yield return new TestCaseData("@=", PaginationOperator.Contains);
            yield return new TestCaseData("@=*", PaginationOperator.ContainsCaseInsensitive);
            yield return new TestCaseData("!@=", PaginationOperator.NotContains);
            yield return new TestCaseData("!@=*", PaginationOperator.NotContainsCaseInsensitive);
            yield return new TestCaseData("_=", PaginationOperator.StartsWith);
            yield return new TestCaseData("_=*", PaginationOperator.StartsWithCaseInsensitive);
            yield return new TestCaseData("!_=", PaginationOperator.NotStartsWith);
            yield return new TestCaseData("!_=*", PaginationOperator.NotStartsWithCaseInsensitive);
            yield return new TestCaseData("_-=", PaginationOperator.EndsWith);
            yield return new TestCaseData("_-=*", PaginationOperator.EndsWithCaseInsensitive);
            yield return new TestCaseData("!_-=", PaginationOperator.NotEndsWith);
            yield return new TestCaseData("!_-=*", PaginationOperator.NotEndsWithCaseInsensitive);
        }

        public static IEnumerable<TestCaseData> CaseInsensitiveFilterCases()
        {
            yield return new TestCaseData("==*", "milk", new[] { 1 });
            yield return new TestCaseData("!=*", "milk", new[] { 2, 3, 4, 5 });
            yield return new TestCaseData("@=*", "milk", new[] { 1, 2, 3 });
            yield return new TestCaseData("!@=*", "milk", new[] { 4, 5 });
            yield return new TestCaseData("_=*", "mi", new[] { 1, 2 });
            yield return new TestCaseData("!_=*", "mi", new[] { 3, 4, 5 });
            yield return new TestCaseData("_-=*", "ilk", new[] { 1, 3 });
            yield return new TestCaseData("!_-=*", "ilk", new[] { 2, 4, 5 });
        }

        [TestCaseSource(nameof(OperatorTokenCases))]
        public void Deserialize_ShouldParseEveryDocumentedOperatorToken(string token, PaginationOperator expectedOperator)
        {
            var deserializer = new PaginationModelDeserializer();
            var model = new PaginationModel { Filters = $"Title{token}value" };

            var result = deserializer.Deserialize(model);

            result.Filters.Count.ShouldBe(1);
            result.Filters[0].Operator.ShouldBe(expectedOperator);
            result.Filters[0].Fields.ShouldBe(new[] { "Title" });
            result.Filters[0].Values.ShouldBe(new[] { "value" });
        }

        [Test]
        public void Deserialize_ShouldSupportGroupedFieldsAndEscapedSpecialCharacters()
        {
            var deserializer = new PaginationModelDeserializer();
            var model = new PaginationModel
            {
                Filters = @"(Title|Description)@=bread\|milk,Title==eq\==ne\!=gt\>lt\<ge\>=le\<=contains\@=starts\_=ends\_-=star\*,Path==c:\\repo\\sample,Title==\null"
            };

            var result = deserializer.Deserialize(model);

            result.Filters.Count.ShouldBe(4);
            result.Filters[0].Fields.ShouldBe(new[] { "Title", "Description" });
            result.Filters[0].Values.ShouldBe(new[] { "bread|milk" });
            result.Filters[1].Values.ShouldBe(new[] { "eq==ne!=gt>lt<ge>=le<=contains@=starts_=ends_-=star*" });
            result.Filters[2].Values.ShouldBe(new[] { @"c:\repo\sample" });
            result.Filters[3].Values.ShouldBe(new[] { @"\null" });
        }

        [TestCaseSource(nameof(CaseInsensitiveFilterCases))]
        public void Apply_ShouldSupportCaseInsensitiveStringOperators(string token, string value, int[] expectedIds)
        {
            var model = new PaginationModel
            {
                Filters = $"Title{token}{value}"
            };

            var source = new List<CompatibilityEntity>
            {
                new() { Id = 1, Title = "Milk" },
                new() { Id = 2, Title = "MILKY" },
                new() { Id = 3, Title = "almond milk" },
                new() { Id = 4, Title = "Bread" },
                new() { Id = 5, Title = null }
            }.AsQueryable();

            var result = Processor.Apply(model, source, applySorting: false, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe(expectedIds);
        }

        [Test]
        public void Apply_ShouldSupportGroupedFieldOrAndMultiValueOr()
        {
            var model = new PaginationModel
            {
                Filters = "(Title|Description)@=milk|bread"
            };

            var source = new List<CompatibilityEntity>
            {
                new() { Id = 1, Title = "fresh milk", Description = "daily" },
                new() { Id = 2, Title = "bagel", Description = "bread basket" },
                new() { Id = 3, Title = "cheese", Description = "aged" },
                new() { Id = 4, Title = "bread", Description = "toast" }
            }.AsQueryable();

            var result = Processor.Apply(model, source, applySorting: false, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe(new[] { 1, 2, 4 });
        }

        [Test]
        public void Apply_ShouldDistinguishNullFromEscapedNullLiteral()
        {
            var source = new List<CompatibilityEntity>
            {
                new() { Id = 1, Title = null },
                new() { Id = 2, Title = "null" },
                new() { Id = 3, Title = "NULL" }
            }.AsQueryable();

            var nullModel = new PaginationModel { Filters = "Title==null" };
            var escapedNullModel = new PaginationModel { Filters = @"Title==\null" };
            var caseInsensitiveEscapedNullModel = new PaginationModel { Filters = @"Title==*\null" };

            var nullResult = Processor.Apply(nullModel, source, applySorting: false, applyPagination: false).Select(x => x.Id).ToList();
            var escapedNullResult = Processor.Apply(escapedNullModel, source, applySorting: false, applyPagination: false).Select(x => x.Id).ToList();
            var caseInsensitiveEscapedResult = Processor.Apply(caseInsensitiveEscapedNullModel, source, applySorting: false, applyPagination: false).Select(x => x.Id).ToList();

            nullResult.ShouldBe(new[] { 1 });
            escapedNullResult.ShouldBe(new[] { 2 });
            caseInsensitiveEscapedResult.ShouldBe(new[] { 2, 3 });
        }

        [Test]
        public void Apply_ShouldSortByMultipleFieldsAndPaginate()
        {
            var model = new PaginationModel
            {
                Sorts = "-created,LikeCount",
                Page = 2,
                PageSize = 2
            };

            var source = new List<CompatibilityEntity>
            {
                new() { Id = 1, Created = 3, LikeCount = 10 },
                new() { Id = 2, Created = 2, LikeCount = 5 },
                new() { Id = 3, Created = 2, LikeCount = 1 },
                new() { Id = 4, Created = 1, LikeCount = 7 },
                new() { Id = 5, Created = 1, LikeCount = 2 }
            }.AsQueryable();

            var result = Processor.Apply(model, source).Select(x => x.Id).ToList();

            result.ShouldBe(new[] { 2, 5 });
        }

        [Test]
        public void Apply_ShouldFilterByNestedProperty()
        {
            var model = new PaginationModel
            {
                Filters = "Creator.Name==*ALICE"
            };

            var source = new List<CompatibilityEntity>
            {
                new() { Id = 1, Creator = new CompatibilityCreator { Name = "alice" } },
                new() { Id = 2, Creator = new CompatibilityCreator { Name = "Alice" } },
                new() { Id = 3, Creator = new CompatibilityCreator { Name = "bob" } }
            }.AsQueryable();

            var result = Processor.Apply(model, source, applySorting: false, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe(new[] { 1, 2 });
        }

        [Test]
        public void Apply_ShouldRespectCanSortForMappedProperties()
        {
            var model = new PaginationModel
            {
                Sorts = "hidden_sort,-created"
            };

            var source = new List<CompatibilityEntity>
            {
                new() { Id = 1, HiddenSort = 1, Created = 1 },
                new() { Id = 2, HiddenSort = 0, Created = 1 },
                new() { Id = 3, HiddenSort = 2, Created = 2 }
            }.AsQueryable();

            var result = Processor.Apply(model, source, applyPagination: false).Select(x => x.Id).ToList();

            result.ShouldBe(new[] { 3, 1, 2 });
        }

        private sealed class CompatibilityEntity
        {
            public int Id { get; init; }

            [Pagination(Name = "title", CanFilter = true, CanSort = true)]
            public string? Title { get; init; }

            [Pagination(Name = "description", CanFilter = true, CanSort = true)]
            public string? Description { get; init; }

            [Pagination(Name = "created", CanFilter = true, CanSort = true)]
            public int Created { get; init; }

            public int LikeCount { get; init; }

            [Pagination(Name = "hidden_sort", CanFilter = false, CanSort = false)]
            public int HiddenSort { get; init; }

            public CompatibilityCreator Creator { get; init; } = new();
        }

        private sealed class CompatibilityCreator
        {
            public string? Name { get; init; }
        }
    }
}
