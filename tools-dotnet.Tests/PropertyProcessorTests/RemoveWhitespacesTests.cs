using NUnit.Framework;
using tools_dotnet.PropertyProcessor;

namespace tools_dotnet.Tests.PropertyProcessorTests
{
    [TestFixture]
    public class RemoveWhitespacesTests
    {
        [TestCase("a b c", "abc")]
        [TestCase("a \"b c\"", "a\"b c\"")]
        public void Test(string input, string expected)
        {
            //Arrange

            //Act
            string output = Util.RemovesWhitespaces(input);

            //Assert
            Assert.That(output, Is.EqualTo(expected));
        }
    }
}