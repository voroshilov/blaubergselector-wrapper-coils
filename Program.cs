using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Owin.Hosting;
using blaubergselector_wrapper_coils.Services;

namespace blaubergselector_wrapper_coils
{
    public static class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        public static void Main(string[] args)
        {
            string rootPath = ConfigurationManager.AppSettings["Coils.RootPath"];

            if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
            {
                AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
                {
                    var name = new AssemblyName(e.Name).Name;
                    var candidate = Path.Combine(rootPath, name + ".dll");
                    return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
                };

                SetDllDirectory(rootPath);
            }

            Run(rootPath);
        }

        private static void Run(string rootPath)
        {
            string url = ConfigurationManager.AppSettings["Host.Url"];
            if (string.IsNullOrWhiteSpace(url))
                url = "http://+:80/";

            string databasePath = ConfigurationManager.AppSettings["Coils.DatabasePath"];

            Console.WriteLine($"Initializing Coils engine from: {rootPath}");
            CoilsEngine.Init(rootPath, databasePath);
            Console.WriteLine($"Database path: {CoilsEngine.DatabasePath}");

            try
            {
                using (WebApp.Start<Startup>(url))
                {
                    Console.WriteLine($"Server listening on {url}");
                    Console.WriteLine("Press Ctrl+C to stop.");

                    var quit = new ManualResetEventSlim(false);
                    Console.CancelKeyPress += (s, e) =>
                    {
                        e.Cancel = true;
                        quit.Set();
                    };
                    quit.Wait();
                }
            }
            finally
            {
                CoilsEngine.Release();
            }
        }
    }
}
