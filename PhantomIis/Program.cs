using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NDesk.Options;

namespace Aranasoft.PhantomIis {
    internal class Program {
        private static int Main(string[] args) {
            ConsoleOptions options;
            try {
                options = new ConsoleOptions(args);
            }
            catch (OptionException e) {
                Console.Error.Write("PhantomIis: ");
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine("Try `PhantomIis --help' for more information.");
                return -1;
            }

            if (options.DisplayVersion) {
                options.ShowVersion();
                return 0;
            }

            if (options.DisplayHelp) {
                options.ShowHelp();
                return 0;
            }

            List<string> validationErrors = options.Validate();
            if (validationErrors.Any()) {
                validationErrors.ForEach(Console.Error.WriteLine);
                return -1;
            }

            return (new Program()).Run(options);
        }

        protected int Run(ConsoleOptions options) {
            Process webServer;
            int phantomResultCode;

            try {
                webServer = StartWebServer(options.IisExpressPath, options.WebsitePath, options.WebsitePort).Result;
                if (webServer == null) {
                    Console.Error.WriteLine("An error occurred while starting IIS express");
                    return -1;
                }
            }
            catch (AggregateException e) {
                Exception firstException = e.InnerExceptions.FirstOrDefault();
                string message = firstException != null ? firstException.Message : string.Empty;
                Console.Error.WriteLine("An error occurred while starting IIS express. " + message);
                return -1;
            }
            catch (Exception) {
                Console.Error.WriteLine("An error occurred while starting IIS express.");
                return -1;
            }

            try {
                phantomResultCode = ExecutePhantomJs(options.PhantomJsPath,
                                                     options.PhantomJsScriptPath,
                                                     options.PhantomJsConfigurationPath);
            }
            catch (AggregateException e) {
                Exception firstException = e.InnerExceptions.FirstOrDefault();
                string message = firstException != null ? firstException.Message : string.Empty;
                Console.Error.WriteLine("An error occurred while executing PhantomJS. " + message);
                StopWebServer(webServer);
                return -1;
            }
            catch (Exception) {
                Console.Error.WriteLine("An error occurred while executing PhantomJS.");
                StopWebServer(webServer);
                return -1;
            }

            StopWebServer(webServer);

            return phantomResultCode;
        }

        private void EnsureProcessExited(Process process) {
            if (process != null && !process.HasExited) process.Kill();
        }

        private int ExecutePhantomJs(string phantomJsExe, string phantomRunnerPath, string phantomConfigurationPath) {
            var completionSource = new TaskCompletionSource<bool>();
            string phantomConfigArg = !string.IsNullOrEmpty(phantomConfigurationPath)
                                          ? string.Format("--config={0}", phantomConfigurationPath)
                                          : string.Empty;
            string phantomJsArgs = string.Format("{1} {0}", phantomRunnerPath, phantomConfigArg);

            var phantomJs = new Process {
                EnableRaisingEvents = true,
                StartInfo = {
                    FileName = phantomJsExe,
                    Arguments = phantomJsArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            int resultCode;

            Action<Process, TaskCompletionSource<bool>> runner = (process, source) => {
                process.OutputDataReceived += (sender, args) => {
                    if (string.Equals(args.Data, "Unable to load the address!", StringComparison.InvariantCulture)) {
                        source.SetException(new ArgumentException(args.Data));
                        return;
                    }
                    Console.WriteLine(args.Data);
                };
                process.Exited += (sender, args) => {
                    Console.WriteLine("PhantomJS finished");
                    source.TrySetResult(process.ExitCode == 0);
                };
                Console.WriteLine("PhantomJS starting");
                process.Start();
                process.BeginOutputReadLine();
            };

            try {
                Task.Run(() => runner(phantomJs, completionSource));
                Task<bool> phantomJsTask = completionSource.Task;
                phantomJsTask.Wait(TimeSpan.FromMinutes(10).Milliseconds);
                resultCode = phantomJsTask.Result ? 0 : -1;
            }
            catch (AggregateException e) {
                string message = e.InnerExceptions.Select(exception => exception.Message).FirstOrDefault();
                Console.WriteLine("An error occurred while executing phantomJs. {0}", message);
                resultCode = -1;
            }

            EnsureProcessExited(phantomJs);

            return resultCode;
        }

        private Task<Process> StartWebServer(string iisExpressPath, string websitePath, uint websitePort) {
            var completionSource = new TaskCompletionSource<Process>();
            string serverArgs = string.Format("/path:{0} /port:{1} /systray:false", websitePath, websitePort);

            var webServer = new Process {
                EnableRaisingEvents = true,
                StartInfo = {
                    CreateNoWindow = true,
                    FileName = iisExpressPath,
                    Arguments = serverArgs,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            Action<Process, TaskCompletionSource<Process>> runner = (process, source) => {
                EventHandler serverFailedOnStart = (sender, args) => source.TrySetCanceled();
                process.Exited += serverFailedOnStart;
                process.OutputDataReceived += (sender, args) => {
                    if (!string.Equals(args.Data, "IIS Express is running.", StringComparison.InvariantCulture)) return;
                    process.Exited -= serverFailedOnStart;
                    source.SetResult(process);
                };
                process.Exited += (sender, args) => Console.WriteLine("IIS Express finished");

                Console.WriteLine("IIS Express starting");
                process.Start();
                process.BeginOutputReadLine();
            };

            Task.Run(() => runner(webServer, completionSource));

            return completionSource.Task;
        }

        private void StopWebServer(Process webServer) {
            if (webServer.HasExited) return;

            for (IntPtr handle = NativeMethods.GetTopWindow(IntPtr.Zero);
                handle != IntPtr.Zero;
                handle = NativeMethods.GetWindow(handle, 2)) {
                uint processId;
                NativeMethods.GetWindowThreadProcessId(handle, out processId);
                if (webServer.Id != processId) continue;
                var handleRef = new HandleRef(null, handle);
                NativeMethods.PostMessage(handleRef, 0x12, IntPtr.Zero, IntPtr.Zero);
                break;
            }

            webServer.WaitForExit(10000);
            EnsureProcessExited(webServer);
        }

        internal class NativeMethods {
            // Methods
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetTopWindow(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool PostMessage(HandleRef hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        }
    }
}
