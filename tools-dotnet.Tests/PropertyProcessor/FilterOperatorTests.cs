using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tools_dotnet.PropertyProcessor;
using tools_dotnet.PropertyProcessor.Filter;

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
            var foundFilter = FilterOperator.Equal.GetFilter<string, string>();

            //Assert
            Assert.That(foundFilter, Is.Not.Null);
            Assert.That(foundFilter.Apply(v1, v1), Is.True);
            Assert.That(foundFilter.Apply(v1, v2), Is.False);
        }
    }
}