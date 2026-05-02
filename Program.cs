using System;
using System.Configuration;
using System.Threading;
using Microsoft.Owin.Hosting;
using blaubergselector_wrapper_coils.Services;

namespace blaubergselector_wrapper_coils
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string url = ConfigurationManager.AppSettings["Host.Url"];
            if (string.IsNullOrWhiteSpace(url))
                url = "http://+:80/";

            var rootPath = ConfigurationManager.AppSettings["Coils.RootPath"];

            Console.WriteLine($"Initializing Coils engine from: {rootPath}");
            CoilsEngine.Init(rootPath);

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
