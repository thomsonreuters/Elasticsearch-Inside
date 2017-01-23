using Nest;
using NUnit.Framework;
using System.IO;

namespace ElasticsearchInside.Tests
{
    [TestFixture]
    public class ElasticsearchTests
    {
        [Test]
        public void Can_start()
        {
            using (var elasticsearch = new Elasticsearch())
            {
                ////Arrange
                var client = new ElasticClient(new ConnectionSettings(elasticsearch.Url));

                ////Act
                var result = client.Ping();

                ////Assert
                Assert.That(result.IsValid);
            }
        }


        [Test]
        public void Can_insert_data()
        {
            using (var elasticsearch = new Elasticsearch())
            {
                ////Arrange
                var client = new ElasticClient(new ConnectionSettings(elasticsearch.Url));

                ////Act
                client.Index(new { id = "tester" }, i => i.Index("test-index").Type("test-type"));

                ////Assert
                var result = client.Get<dynamic>("tester", "test-index", "test-type");
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Found);
            }
        }

        [Test]
        public void Can_change_configuration()
        {
            using (var elasticsearch = new Elasticsearch(c => c.Port(444).EnableLogging()))
            {
                ////Arrange
                var client = new ElasticClient(new ConnectionSettings(elasticsearch.Url));

                ////Act
                var result = client.Ping();

                ////Assert
                Assert.That(result.IsValid);
                Assert.That(elasticsearch.Url.Port, Is.EqualTo(444));
            }
        }

        [Test]
        public void Can_log_output()
        {
            var logged = false;
            using (new Elasticsearch(c => c.EnableLogging().LogTo((f, a) => logged = true)))
            {
                ////Assert
                Assert.That(logged);
            }
        }

        [Test]
        public void Can_install_plugin()
        {
            string pluginName = "mapper-size";
            using (var elasticsearch = new Elasticsearch(c => c.AddPlugin(new Configuration.Plugin(pluginName))))
            {
                ////Arrange
                var client = new ElasticClient(new ConnectionSettings(elasticsearch.Url));

                ////Act
                var result = client.CatPlugins();

                int pluginCount = 0;
                foreach (CatPluginsRecord plugin in result.Records)
                {
                    pluginCount++;
                }

                ////Assert
                Assert.That(result.IsValid);
                Assert.AreEqual(1, pluginCount);
            }
        }

        [Test]
        public void Can_install_plugin_url()
        {
            string pluginName = "mapper-size";
            string pluginUrl = "file:///" + Directory.GetCurrentDirectory() + "\\TestFiles\\mapper-size-5.0.0.zip";
            using (var elasticsearch = new Elasticsearch(c => c.EnableLogging().AddPlugin(new Configuration.Plugin(pluginName, pluginUrl))))
            {
                ////Arrange
                var client = new ElasticClient(new ConnectionSettings(elasticsearch.Url));

                ////Act
                var result = client.CatPlugins();

                int pluginCount = 0;
                foreach (CatPluginsRecord plugin in result.Records)
                {
                    pluginCount++;
                }

                var stats = client.ClusterStats();

                ////Assert
                Assert.That(result.IsValid);
                Assert.AreEqual(1, pluginCount);
            }
        }
    }
}
