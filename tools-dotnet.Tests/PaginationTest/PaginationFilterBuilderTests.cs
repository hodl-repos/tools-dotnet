using Shouldly;
using tools_dotnet.Pagination.Attributes;
using tools_dotnet.Pagination.Builders;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class PaginationFilterBuilderTests
    {
        [TestCaseSource(nameof(AllOperators))]
        public void And_ShouldBuildExplicitFilter_ForEveryPaginationOperator(
            PaginationOperator op
        )
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .And(x => x.Name, "value", op)
                .Build();

            filters.ShouldBe($"name{op.Id}value");
        }

        [TestCaseSource(nameof(AllOperators))]
        public void And_ShouldRoundTripExplicitFilter_ForEveryPaginationOperator(
            PaginationOperator op
        )
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .And(x => x.Name, "value", op)
                .Build();

            var result = new PaginationModelDeserializer()
                .Deserialize(new PaginationModel { Filters = filters });

            result.Filters.Count.ShouldBe(1);
            result.Filters[0].Fields.ShouldBe(["name"]);
            result.Filters[0].Operator.ShouldBe(op);
            result.Filters[0].Values.ShouldBe(["value"]);
        }

        [Test]
        public void And_ShouldParseSimpleBinaryExpressions()
        {
            var cutoff = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

            var filters = CreateFilter<FilterBuilderEntity>
                .And(x => x.Age == 5)
                .And(x => x.Age != 6)
                .And(x => x.Age > 7)
                .And(x => x.Age >= 8)
                .And(x => x.Age < 9)
                .And(x => x.Age <= 10)
                .And(x => x.CreatedAt >= cutoff)
                .Build();

            filters.ShouldBe(
                "age==5,age!=6,age>7,age>=8,age<9,age<=10,created_at>=2030-01-02T03:04:05.0000000+00:00"
            );
        }

        [Test]
        public void And_ShouldSupportConstantOnLeftSide()
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .And(x => 5 < x.Age)
                .And(x => 10 >= x.Age)
                .Build();

            filters.ShouldBe("age>5,age<=10");
        }

        [Test]
        public void And_ShouldUsePaginationAttributeNames_ForNestedMemberPaths()
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .And(x => x.Profile.DisplayName, "Alice", PaginationOperator.Equal)
                .Build();

            filters.ShouldBe("profile.display_name==Alice");
        }

        [Test]
        public void AndAny_ShouldBuildGroupedFieldOr()
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .AndAny(
                    [x => x.Name, x => x.Profile.DisplayName],
                    "smith",
                    PaginationOperator.ContainsCaseInsensitive
                )
                .Build();

            filters.ShouldBe("(name|profile.display_name)@=*smith");
        }

        [Test]
        public void And_ShouldBuildMultiValueOr()
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .AndValues(
                    x => x.Status,
                    ["active", "pending"],
                    PaginationOperator.Equal
                )
                .Build();

            filters.ShouldBe("status==active|pending");
        }

        [Test]
        public void Build_ShouldEscapeValues_ForPaginationSyntax()
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .And(x => x.Name, @"bread,milk|tea\water(null)*!@_=><", PaginationOperator.Equal)
                .Build();

            filters.ShouldBe(@"name==bread\,milk\|tea\\water\(null\)\*\!\@\_\=\>\<");
        }

        [Test]
        public void Build_ShouldEscapeNullString_ButLeaveNullLiteral()
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .And(x => x.Name, "null", PaginationOperator.Equal)
                .And(x => x.OptionalName, null, PaginationOperator.Equal)
                .Build();

            filters.ShouldBe(@"name==\null,optional_name==null");
        }

        [Test]
        public void Build_ShouldRoundTripThroughPaginationModelDeserializer()
        {
            var filters = CreateFilter<FilterBuilderEntity>
                .AndAny([x => x.Name, x => x.Profile.DisplayName], "a,b|c", PaginationOperator.Contains)
                .AndValues(x => x.Age, [5, 6], PaginationOperator.GreaterThanOrEqual)
                .Build();

            var result = new PaginationModelDeserializer()
                .Deserialize(new PaginationModel { Filters = filters });

            result.Filters.Count.ShouldBe(2);
            result.Filters[0].Fields.ShouldBe(["name", "profile.display_name"]);
            result.Filters[0].Operator.ShouldBe(PaginationOperator.Contains);
            result.Filters[0].Values.ShouldBe(["a,b|c"]);
            result.Filters[1].Fields.ShouldBe(["age"]);
            result.Filters[1].Operator.ShouldBe(PaginationOperator.GreaterThanOrEqual);
            result.Filters[1].Values.ShouldBe(["5", "6"]);
        }

        [Test]
        public void ToPaginationModel_ShouldBuildPaginationModelWithFiltersAndPaging()
        {
            var model = CreateFilter<FilterBuilderEntity>
                .And(x => x.Age >= 18)
                .ToPaginationModel(page: 2, pageSize: 25);

            model.Filters.ShouldBe("age>=18");
            model.Page.ShouldBe(2);
            model.PageSize.ShouldBe(25);
        }

        [Test]
        public void And_ShouldRejectUnsupportedExpressions()
        {
            Should.Throw<ArgumentException>(() =>
                CreateFilter<FilterBuilderEntity>.And(x => x.Name!.Contains("a"))
            );
        }

        [Test]
        public void And_ShouldRejectMemberToMemberComparisons()
        {
            var exception = Should.Throw<ArgumentException>(() =>
                CreateFilter<FilterBuilderEntity>.And(x => x.Age == x.OtherAge)
            );

            exception.Message.ShouldContain("cannot reference the filter entity");
        }

        [Test]
        public void AndValues_ShouldRejectEmptyValueCollections()
        {
            var exception = Should.Throw<ArgumentException>(() =>
                CreateFilter<FilterBuilderEntity>.AndValues(
                    x => x.Age,
                    Array.Empty<int>(),
                    PaginationOperator.Equal
                )
            );

            exception.ParamName.ShouldBe("values");
        }

        public static IEnumerable<TestCaseData> AllOperators()
        {
            return PaginationOperator.Values.Select(op => new TestCaseData(op).SetName(op.Name));
        }

        private sealed class FilterBuilderEntity
        {
            [Pagination(Name = "name")]
            public string? Name { get; init; }

            [Pagination(Name = "optional_name")]
            public string? OptionalName { get; init; }

            [Pagination(Name = "age")]
            public int Age { get; init; }

            [Pagination(Name = "other_age")]
            public int OtherAge { get; init; }

            [Pagination(Name = "created_at")]
            public DateTimeOffset CreatedAt { get; init; }

            [Pagination(Name = "status")]
            public string Status { get; init; } = string.Empty;

            [Pagination(Name = "profile", CanFilterSubProperties = true)]
            public FilterBuilderProfile Profile { get; init; } = new();
        }

        private sealed class FilterBuilderProfile
        {
            [Pagination(Name = "display_name")]
            public string? DisplayName { get; init; }
        }
    }
}
