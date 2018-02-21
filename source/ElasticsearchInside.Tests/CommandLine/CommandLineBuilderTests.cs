using ElasticsearchInside.CommandLine;
using NUnit.Framework;

namespace ElasticsearchInside.Tests.CommandLine
{
    [TestFixture]
    public class CommandLineBuilderTests
    {
        [Test]
        public void Can_add_argument()
        {
            ////Arrange
            var builder = new CommandLineBuilder();

            ////Act
            var result = builder.Build(new Example {Argument = "tester"});

            ////Assert
            Assert.That(result, Is.EqualTo(" -Des.index.gateway.type=tester"));
        }

        private class Example
        {
            [FormattedArgument("-Des.index.gateway.type={0}")]
            public string Argument { get; set; }
        }

        [Test]
        public void Build_ElasticsearchParameter_Custom()
        {
            ////Arrange
            var builder = new CommandLineBuilder();

            ////Act
            var ep = new ElasticsearchParameters();
            ep.AddJavaArgument("-agentlib:jdwp=transport=dt_socket,server=y,suspend=y,address=8000");
            var result = builder.Build(ep);
        }
    }
}