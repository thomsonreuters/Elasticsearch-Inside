﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElasticsearchInside.CommandLine;
using ElasticsearchInside.Executables;
using ElasticsearchInside.Utilities.Archive;
using LZ4PCL;
using ElasticsearchInside.Configuration;

namespace ElasticsearchInside
{
    public class Elasticsearch : IDisposable
    {
        private Process _elasticSearchProcess;
        private bool _disposed;
        private readonly DirectoryInfo temporaryRootFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        private DirectoryInfo ElasticsearchHome { get; set; }
        private DirectoryInfo JavaHome { get; set; }
        private readonly ElasticsearchParameters parameters = new ElasticsearchParameters();
        private readonly CommandLineBuilder _commandLineBuilder = new CommandLineBuilder();
        private readonly Stopwatch startup;


        static Elasticsearch()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                using (var memStream = new MemoryStream())
                {
                    using (var stream = typeof(Elasticsearch).Assembly.GetManifestResourceStream(typeof(RessourceTarget), "LZ4PCL.dll"))
                        stream.CopyTo(memStream);

                    return Assembly.Load(memStream.GetBuffer());
                }
            };
        }

        public Uri Url
        {
            get
            {
                if (!parameters.ElasticsearchPort.HasValue)
                    throw new ApplicationException("Expected HttpPort to be set");

                return new UriBuilder
                {
                    Scheme = Uri.UriSchemeHttp,
                    Host = parameters.NetworkHost,
                    Port = parameters.ElasticsearchPort.Value
                }.Uri;
            }
        }

        private void Info(string format, params object[] args)
        {
            if (parameters.LoggingEnabled)
                if (args == null || args.Length == 0)
                    parameters.Logger("{0}", new object[] { format });
                else
                    parameters.Logger(format, args);
        }


        public Elasticsearch(Func<IElasticsearchParameters, IElasticsearchParameters> configurationAction = null)
        {
            if (configurationAction != null)
                configurationAction.Invoke(parameters);

            startup = Stopwatch.StartNew();

            SetupEnvironment();

            Info("Environment ready after {0} seconds", startup.Elapsed.TotalSeconds);

            StartProcess();
            WaitForGreen();

            InstallPlugins();
        }

        private void InstallPlugins()
        {
            foreach (Plugin plugin in parameters.Plugins)
            {
                Process proc = new Process();
                proc.StartInfo.FileName = Path.Combine(ElasticsearchHome.FullName, "bin\\elasticsearch-plugin.bat");
                proc.StartInfo.WorkingDirectory = Path.Combine(ElasticsearchHome.FullName, "bin");
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.OutputDataReceived += (sender, args) => Debug.WriteLine(plugin.Name + ":INFO: " + args.Data);
                proc.ErrorDataReceived += (sender, args) => Debug.WriteLine(plugin.Name + ":ERROR: " + args.Data);
                proc.StartInfo.Arguments = plugin.GetInstallCommand();

                // set JAVA_HOME to use the packaged JRE
                const string JAVA_HOME = "JAVA_HOME";
                if (proc.StartInfo.EnvironmentVariables.ContainsKey(JAVA_HOME))
                {
                    Info("Removing old JAVA_HOME and replacing with bundled JRE.");
                    proc.StartInfo.EnvironmentVariables.Remove(JAVA_HOME);
                }
                proc.StartInfo.EnvironmentVariables.Add(JAVA_HOME, JavaHome.FullName);

                Info("Installing plugin " + plugin.Name + "...");
                Info("    " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                Info("Waiting for plugin " + plugin.Name + " install...");
                proc.WaitForExit();
                Info($"Plugin {plugin.Name} install completed [exit-code {proc.ExitCode}].");
            }

            Restart();
        }

        private void SetupEnvironment()
        {
            parameters.EsHomePath = new DirectoryInfo(Path.Combine(temporaryRootFolder.FullName, "es"));
            JavaHome = new DirectoryInfo(Path.Combine(temporaryRootFolder.FullName, "jre"));
            ElasticsearchHome = parameters.EsHomePath;
            parameters.EsHomePath = ElasticsearchHome;
            

            var jreTask = Task.Run(() => ExtractEmbeddedLz4Stream("jre.lz4", JavaHome));
            var esTask = Task.Run(() => ExtractEmbeddedLz4Stream("elasticsearch.lz4", ElasticsearchHome));

            Task.WaitAll(jreTask, esTask);
        }


        private void WaitForGreen()
        {
            var statusUrl = new UriBuilder(Url)
            {
                Path = "_cluster/health",
                Query = "wait_for_status=yellow"
            }.Uri;

            var statusCode = (HttpStatusCode)0;
            do
            {
                try
                {
                    var request = WebRequest.Create(statusUrl);
                    using (var response = (HttpWebResponse)request.GetResponse())
                        statusCode = response.StatusCode;
                }
                catch (WebException)
                {
                }

                Thread.Sleep(100);

            } while (statusCode != HttpStatusCode.OK);

            startup.Stop();
            Info("Started in {0} seconds", startup.Elapsed.TotalSeconds);
        }

        private void StartProcess()
        {
            var processStartInfo = new ProcessStartInfo(string.Format(@"""{0}""", Path.Combine(JavaHome.FullName, "bin/java.exe")))
            {
                UseShellExecute = false,
                Arguments = _commandLineBuilder.Build(parameters),
                WindowStyle = ProcessWindowStyle.Maximized,
                CreateNoWindow = true,
                LoadUserProfile = false,
                WorkingDirectory = ElasticsearchHome.FullName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.ASCII,
            };

            Info("Folder {0}", processStartInfo.WorkingDirectory);
            Info("Starting {0}", processStartInfo.FileName);
            Info("Arguments {0}", processStartInfo.Arguments);


            _elasticSearchProcess = Process.Start(processStartInfo);
            _elasticSearchProcess.ErrorDataReceived += (sender, eventargs) => Info(eventargs.Data);
            _elasticSearchProcess.OutputDataReceived += (sender, eventargs) => Info(eventargs.Data);
            _elasticSearchProcess.BeginOutputReadLine();
            _elasticSearchProcess.BeginErrorReadLine();
        }

        public void Restart()
        {
            _elasticSearchProcess.Kill();
            _elasticSearchProcess.WaitForExit();

            StartProcess();
            WaitForGreen();
        }

        private void ExtractEmbeddedLz4Stream(string name, DirectoryInfo destination)
        {
            var started = Stopwatch.StartNew();

            using (var stream = GetType().Assembly.GetManifestResourceStream(typeof(RessourceTarget), name))
            using (var decompresStream = new LZ4Stream(stream, CompressionMode.Decompress))
            using (var archiveReader = new ArchiveReader(decompresStream))
                archiveReader.ExtractToDirectory(destination);
           
            Info("Extracted {0} in {1} seconds", name, started.Elapsed.TotalSeconds);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            try
            {
                _elasticSearchProcess.Kill();
                _elasticSearchProcess.WaitForExit();
                temporaryRootFolder.Delete(true);

            }
            catch (Exception ex)
            {
                Info(ex.ToString());
            }
            _disposed = true;

        }

        ~Elasticsearch()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
