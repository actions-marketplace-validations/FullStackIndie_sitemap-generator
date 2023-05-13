﻿using McMaster.Extensions.CommandLineUtils;
using System.Text;
using System.Threading;

namespace SiteMapGenerator
{
    [Command(Name = "sitemap", Description =
        "Create a XML SiteMap for your website so Search Engines can find your site.")]
    class Program
    {
        static Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        [Argument(0, Description = "Your website domain to crawl")]
        private string Url { get; }

        //[Option("-L|--log", Description = "Path to save logs.")]
        //public string LogPath { get; }

        [Option("-L|--log", Description = "Path to save logs. Defaults to current Directory. Saves as sitemap_generator_logs.txt")]
        public (bool hasValue, string value) LogPath { get; }


        public static StringBuilder Logger { get; set; } = new();

        async Task<int> OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken = default)
        {
            Logger.AppendLine();
            Logger.AppendLine($"--- Starting sitemap generator. DateTime UTC = {DateTime.UtcNow} ---");
            if (string.IsNullOrEmpty(Url))
            {
                Logger.LogError($"Url was empty");
                app.ShowHelp();
                return 0;
            }
            var crawler = new WebCrawler();
            Uri uri = new(Url);
            Logger.Log($"Crawling {uri}");

            await crawler.Crawl(uri, uri, cancellationToken);
            await crawler.GenerateSitemapAsync(cancellationToken);
            Logger.Log("Crawling complete.");

            if (File.Exists($"{Directory.GetCurrentDirectory()}/sitemap.xml"))
            {
                Logger.Log($"Sitemap generated at {Directory.GetCurrentDirectory().Replace(Path.DirectorySeparatorChar, '/')}/sitemap.xml", consoleColor: ConsoleColor.Green);
                SaveLogs(Logger);
                return 0;
            }
            Logger.LogError($"There was an error while creating the sitemap for {Url}");
            SaveLogs(Logger);
            return 1;
        }


        private void SaveLogs(StringBuilder logs)
        {
            if (!LogPath.hasValue)
            {
                logs.LogError("Log Path was not provided, skipped saving logs....");
                return;
            }
            if (string.IsNullOrEmpty(LogPath.value) || !Directory.Exists(LogPath.value) && LogPath.value != ".")
            {
                logs.LogError($"Log Path {LogPath.value} was invalid. Make sure Directory exists, skipped saving logs....");
                return;
            }
            if(LogPath.value == ".")
            {
                logs.Log($"Saving logs to {Directory.GetCurrentDirectory().Replace(Path.DirectorySeparatorChar, '/')}/sitemap_generator_logs.txt", consoleColor: ConsoleColor.Green);
                File.AppendAllText($"{Directory.GetCurrentDirectory().Replace(Path.DirectorySeparatorChar, '/')}/sitemap_generator_logs.txt", logs.ToString());
                return;
            }
            logs.Log($"Saving logs to {LogPath.value.TrimEnd('/').Replace(Path.DirectorySeparatorChar, '/')}/sitemap_generator_logs.txt", consoleColor: ConsoleColor.Green);
            File.AppendAllText($"{LogPath.value.TrimEnd('/').Replace(Path.DirectorySeparatorChar, '/')}/sitemap_generator_logs.txt", logs.ToString());
        }

    }
}