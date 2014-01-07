using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NDesk.Options;

namespace Aranasoft.PhantomIis {
    internal class ConsoleOptions {
        private OptionSet _options;

        public ConsoleOptions(IEnumerable<string> args) {
            IisExpressPath = null;
            WebsitePath = Environment.CurrentDirectory;
            WebsitePort = 3000;
            PhantomJsPath = null;
            PhantomJsScriptPath = "phantom.run.js";
            PhantomJsConfigurationPath = null;
            DisplayVersion = false;
            DisplayHelp = false;
            Options.Parse(args);
        }

        public bool DisplayVersion { get; protected set; }
        public bool DisplayHelp { get; protected set; }
        public string WebsitePath { get; protected set; }
        public uint WebsitePort { get; protected set; }
        public string IisExpressPath { get; protected set; }
        public string PhantomJsPath { get; protected set; }
        public string PhantomJsConfigurationPath { get; protected set; }
        public string PhantomJsScriptPath { get; protected set; }

        public OptionSet Options {
            get { return _options ?? (_options = InitializeOptions()); }
        }

        private OptionSet InitializeOptions() {
            return new OptionSet {
                {
                    "i|iisexpress=", "Location of iisexpress.exe\n(Default: find using PATH environment)",
                    value => IisExpressPath = value
                }, {
                    "j|phantomjs=", "Location of PhantomJs.exe\n(Default: find using PATH environment)",
                    value => PhantomJsPath = value
                }, {
                    "jc|phantomconfig=", "Location of PhantomJs Configuration\n(Example: .\\phantomjs.json)",
                    value => PhantomJsConfigurationPath = value
                }, {
                    "js|phantomscript=", "Location of PhantomJs script\n(Default: .\\phantom.run.js)",
                    value => PhantomJsScriptPath = value
                }, {
                    "s|siteroot=", "Location of web site root\n(Default: .\\)",
                    value => WebsitePath = value
                }, {
                    "p|port=", "Port to launch web site on\n(Default: 3000)",
                    new Action<uint>(value => WebsitePort = value)
                }, {
                    "h|help", "Show usage information",
                    value => DisplayHelp = value != null
                }, {
                    "V|version", "Show the version number",
                    value => DisplayVersion = value != null
                },
            };
        }

        public List<string> Validate() {
            var errors = new List<string>();

            IisExpressPath = GetFullExePath("iisexpress.exe", IisExpressPath);
            if (IisExpressPath == null) errors.Add("Error: Unable to find iisexpress.exe");

            PhantomJsPath = GetFullExePath("phantomjs.exe", PhantomJsPath);
            if (PhantomJsPath == null) errors.Add("Error: Unable to find phantomjs.exe");

            if (!File.Exists(PhantomJsScriptPath)) errors.Add(string.Format("Error: Unable to find PhantomJS script: {0}", PhantomJsScriptPath));

            if (!string.IsNullOrEmpty(PhantomJsConfigurationPath) && !File.Exists(PhantomJsConfigurationPath)) {
                errors.Add(string.Format("Error: Unable to find phantomJS configuration file: {0}",
                                         PhantomJsConfigurationPath));
            }

            return errors;
        }

        public void ShowVersion() {
            Assembly thisAssembly = Assembly.GetEntryAssembly();
            FileVersionInfo assemblyVersionInfo = FileVersionInfo.GetVersionInfo(thisAssembly.Location);
            Console.WriteLine("{0}.{1}.{2}",
                              assemblyVersionInfo.ProductMajorPart,
                              assemblyVersionInfo.ProductMinorPart,
                              assemblyVersionInfo.ProductBuildPart);
        }

        public void ShowHelp() {
            Console.WriteLine();
            Console.WriteLine("Usage: PhantomIis [options]");
            Console.WriteLine("Run PhantomJs within the context of an IIS Express web server");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
        }

        private string GetFullExePath(string executable, string path) {
            if (!string.IsNullOrWhiteSpace(path)) return File.Exists(path) ? path : null;

            return FindExecutableFromEnvironment(executable);
        }

        private string FindExecutableFromEnvironment(string executable) {
            return GetEnvironmentPaths()
                .Select(envPath => Path.Combine(envPath, executable))
                .FirstOrDefault(File.Exists);
        }

        private IEnumerable<string> GetEnvironmentPaths() {
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            return path.Split(';');
        }
    }
}
