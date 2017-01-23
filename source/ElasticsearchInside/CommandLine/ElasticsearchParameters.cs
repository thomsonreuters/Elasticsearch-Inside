﻿using ElasticsearchInside.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ElasticsearchInside.CommandLine
{
    internal class ElasticsearchParameters : IElasticsearchParameters
    {
        private static readonly Random Random = new Random();

        private Action<string, object[]> _logger = (format, args) => Trace.WriteLine(string.Format(format, args));

        private IList<string> _customCommandlineArguments = new List<string>();
        public ElasticsearchParameters()
        {
            ElasticsearchPort = Random.Next(49152, 65535 + 1);
            EsNodeName = "elasticsearch_" + ElasticsearchPort;
            Clustername = EsNodeName;
            NetworkHost = "127.0.0.1";
        }

        [FormattedArgument("-Xms{0}m", 128)]
        public int? InitialHeapSize { get; set; }

        [FormattedArgument("-Xmx{0}m", 128)]
        public int? MaximumHeapSize { get; set; }

        [FormattedArgument("-Djava.awt.headless={0}", "true")]
        public BooleanParameter Headless { get; set; }

        [BooleanArgument("-XX:+UseParNewGC", true)]
        public BooleanParameter EnableAParallelYoungGenerationGCwithTheConcurrentGC { get; set; }

        [BooleanArgument("-XX:+UseConcMarkSweepGC", true)]
        public BooleanParameter UseConcurrentMarkSweepGC { get; set; }

        [FormattedArgument("-XX:CMSInitiatingOccupancyFraction={0}", 75)]
        public int? CMSInitiatingOccupancyFraction { get; set; }

        [BooleanArgument("-XX:+UseCMSInitiatingOccupancyOnly", true)]
        public BooleanParameter UseCMSInitiatingOccupancyOnly { get; set; }

        [BooleanArgument("-XX:+HeapDumpOnOutOfMemoryError", true)]
        public BooleanParameter HeapDumpOnOutOfMemoryError { get; set; }

        [BooleanArgument("-XX:+DisableExplicitGC", true)]
        public BooleanParameter DisableExplicitGC { get; set; }

        [FormattedArgument("-Dfile.encoding={0}", "UTF-8")]
        public string FileEncoding { get; set; }

        [BooleanArgument("-Delasticsearch", true)]
        internal string Elasticsearch { get; set; }

        [FormattedArgument("-Des-foreground={0}", "yes")]
        public YesNoParameter EsForeground { get; set; }

        [FormattedArgument("-Des.path.home=\"{0}\"")]
        public DirectoryInfo EsHomePath { get; set; }

        [BooleanArgument(@"-cp ""lib/*""", true)]
        internal object ClassPath { get; set; }

        [BooleanArgument("\"org.elasticsearch.bootstrap.Elasticsearch\"", true)]
        internal object JarFile { get; set; }

        [FormattedArgument("-Ehttp.port={0}", 1234)]
        public int? ElasticsearchPort { get; set; }

        [FormattedArgument("-Enode.name={0}", "integrationtest_node")]
        public string EsNodeName { get; set; }

        [FormattedArgument("-Epath.conf={0}", "config")]
        public DirectoryInfo ConfigPath { get; set; }

        [FormattedArgument("-Escript.file={0}", "true")]
        public OnOffParameter ElasticsearchScriptFile { get; set; }

        [FormattedArgument("-Ediscovery.zen.minimum_master_nodes={0}", 1)]
        public int MinimumMasterNodes { get; set; }

        [FormattedArgument("-Etransport.type={0}", "local")]
        public string TransportType { get; set; }

        [FormattedArgument("-Ecluster.name=integrationtest_{0}", "tester")]
        public string Clustername { get; set; }

        [FormattedArgument("-Enetwork.host={0}")]
        public string NetworkHost { get; set; }

        [FormattedArgument("{0}")]
        public string CustomArguments
        {
            get { return string.Join(" ", _customCommandlineArguments); }
        }

        internal List<Plugin> Plugins { get; } = new List<Plugin>();

        public IElasticsearchParameters HeapSize(int initialHeapsizeMB = 128, int maximumHeapsizeMB = 128)
        {
            InitialHeapSize = initialHeapsizeMB;
            MaximumHeapSize = maximumHeapsizeMB;
            return this;
        }


        public IElasticsearchParameters Port(int port)
        {
            ElasticsearchPort = port;
            return this;
        }

        public IElasticsearchParameters EnableLogging(bool enable = true)
        {
            this.LoggingEnabled = enable;
            return this;
        }

        public bool LoggingEnabled { get; set; }

        public Action<string, object[]> Logger
        {
            get { return _logger; }
        }

        public IElasticsearchParameters LogTo(Action<string, object[]> logger)
        {
            this._logger = logger;
            return this;
        }


        public IElasticsearchParameters AddArgument(string argument)
        {
            _customCommandlineArguments.Add(argument);
            return this;
        }

        public IElasticsearchParameters AddPlugin(Plugin plugin)
        {
            Plugins.Add(plugin);
            return this;
        }
    }
}
