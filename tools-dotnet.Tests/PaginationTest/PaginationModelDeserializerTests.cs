using Shouldly;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Tests.PaginationTest
{
    [TestFixture]
    public class PaginationModelDeserializerTests
    {
        private readonly PaginationModelDeserializer _deserializer = new();

        [Test]
        public void Deserialize_ShouldParseFiltersAndSorts()
        {
            var model = new PaginationModel
            {
                Filters = @"firstName|lastName@=jo\,hn,age>=18,status==active|pending,path==a\|b",
                Sorts = "-createdAt,+firstName,address.city",
                Page = 2,
                PageSize = 50
            };

            var result = _deserializer.Deserialize(model);

            result.Page.ShouldBe(2);
            result.PageSize.ShouldBe(50);
            result.Filters.Count.ShouldBe(4);
            result.Sorts.Count.ShouldBe(3);

            var namesFilter = result.Filters[0];
            namesFilter.Fields.Count.ShouldBe(2);
            namesFilter.Fields[0].ShouldBe("firstName");
            namesFilter.Fields[1].ShouldBe("lastName");
            namesFilter.Operator.ShouldBe(PaginationOperator.Contains);
            namesFilter.Values.Count.ShouldBe(1);
            namesFilter.Values[0].ShouldBe("jo,hn");

            var ageFilter = result.Filters[1];
            ageFilter.Operator.ShouldBe(PaginationOperator.GreaterThanOrEqual);
            ageFilter.Values[0].ShouldBe("18");

            var statusFilter = result.Filters[2];
            statusFilter.Operator.ShouldBe(PaginationOperator.Equal);
            statusFilter.Values.Count.ShouldBe(2);
            statusFilter.Values[0].ShouldBe("active");
            statusFilter.Values[1].ShouldBe("pending");

            var escapedPipeFilter = result.Filters[3];
            escapedPipeFilter.Values.Count.ShouldBe(1);
            escapedPipeFilter.Values[0].ShouldBe("a|b");

            result.Sorts[0].Field.ShouldBe("createdAt");
            result.Sorts[0].Descending.ShouldBeTrue();
            result.Sorts[1].Field.ShouldBe("firstName");
            result.Sorts[1].Descending.ShouldBeFalse();
            result.Sorts[2].Field.ShouldBe("address.city");
            result.Sorts[2].Descending.ShouldBeFalse();
        }

        [Test]
        public void Deserialize_ShouldNormalizePageDefaults()
        {
            var model = new PaginationModel
            {
                Filters = "name==test",
                Page = 0,
                PageSize = -5
            };

            var result = _deserializer.Deserialize(model);

            result.Page.ShouldBe(1);
            result.PageSize.ShouldBe(25);
        }

        [Test]
        public void Deserialize_ShouldIgnoreInvalidTerms()
        {
            var model = new PaginationModel
            {
                Filters = "name~invalid,age>=21,empty==,==missingField",
                Sorts = ",,-createdAt"
            };

            var result = _deserializer.Deserialize(model);

            result.Filters.Count.ShouldBe(2);
            result.Filters[0].Fields[0].ShouldBe("age");
            result.Filters[0].Operator.ShouldBe(PaginationOperator.GreaterThanOrEqual);
            result.Filters[1].Fields[0].ShouldBe("empty");
            result.Filters[1].Operator.ShouldBe(PaginationOperator.Equal);
            result.Sorts.Count.ShouldBe(1);
            result.Sorts[0].Field.ShouldBe("createdAt");
        }

        [Test]
        public void TryFromToken_ShouldResolveOperatorUsingEnumExt()
        {
            var parsed = PaginationOperator.TryFromToken("!@=", out var op);

            parsed.ShouldBeTrue();
            op.ShouldNotBeNull();
            op.ShouldBe(PaginationOperator.NotContains);
            PaginationOperator.Values.Count.ShouldBeGreaterThan(5);
        }
    }
}
