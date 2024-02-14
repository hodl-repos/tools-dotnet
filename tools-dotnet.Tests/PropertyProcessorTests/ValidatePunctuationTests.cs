using NUnit.Framework;
using tools_dotnet.PropertyProcessor;

namespace tools_dotnet.Tests.PropertyProcessorTests
{
    [TestFixture]
    public class ValidatePunctuationTests
    {
        [TestCase("a b c", null)]
        [TestCase("a \"b c\"", null)]
        [TestCase("(()[[{{}(\"\\\"})]\")}]])", null)]
        [TestCase("}", 0)]
        [TestCase("{]", 1)]
        [TestCase("{", 0)]
        [TestCase("{}", null)]
        [TestCase("\"", 0)]
        public void Test(string input, long? expected)
        {
            //Arrange

            //Act
            long? output = Util.ValidatePunctuation(input);

            //Assert
            Assert.That(output, Is.EqualTo(expected));
        }
    }
}