using NUnit.Framework;
using tools_dotnet.PropertyProcessor;

namespace tools_dotnet.Tests.PropertyProcessor
{
    [TestFixture]
    public class FilterOperatorTests
    {
        [TestCase]
        public void TestEquals()
        {
            //Arrange
            string v1 = "abc";
            string v2 = "def";

            //Act
            var foundFilter = FilterOperatorEnum.Equal.GetFilter<string, string>();

            //Assert
            Assert.That(foundFilter, Is.Not.Null);
            Assert.That(foundFilter.Apply(v1, v1), Is.True);
            Assert.That(foundFilter.Apply(v1, v2), Is.False);
        }

        [TestCase]
        public void TestAnyStartsWith()
        {
            //Arrange
            string[] v1 = ["abc", "def"];
            string[] v2 = ["ab"];

            //Act

            //Assert
            Assert.That(FilterOperatorEnum.Apply("_=", v1 as ICollection<string>, new string[] { "ab" } as ICollection<string>), Is.Not.Null);
            Assert.That(FilterOperatorEnum.Apply("_=", v1 as ICollection<string>, new string[] { "ab" } as ICollection<string>), Is.True);
            Assert.That(FilterOperatorEnum.Apply("!_=", v1 as ICollection<string>, new string[] { "ab" } as ICollection<string>), Is.False);

            Assert.That(FilterOperatorEnum.Apply("!_=", v1 as ICollection<string>, new string[] { "rr" } as ICollection<string>), Is.Not.Null);
            Assert.That(FilterOperatorEnum.Apply("!_=", v1 as ICollection<string>, new string[] { "rr" } as ICollection<string>), Is.True);
            Assert.That(FilterOperatorEnum.Apply("_=", v1 as ICollection<string>, new string[] { "rr" } as ICollection<string>), Is.False);
        }
    }
}