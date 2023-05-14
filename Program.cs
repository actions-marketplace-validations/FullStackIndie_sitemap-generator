using McMaster.Extensions.CommandLineUtils;
using System.Text;

namespace SiteMapGenerator
{
    [Command(Name = "sitemap", Description =
        "Create a XML SiteMap for your website so Search Engines can find your site.")]
    class Program
    {
        static Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        [Argument(0, Description = "Your website domain/url to crawl")]
        private string Url { get; }

        [Option("-P|--path", Description = "Directory to save sitemap. Defaults to current Directory. Saves as sitemap.xml")]
        public string? SiteMapPath { get; }

        [Option("-L|--log-path", Description = "Directory to save logs. Defaults to current Directory. Saves as sitemap_generator_logs.txt")]
        public (bool hasValue, string value) LogPath { get; }


        public static StringBuilder Logger { get; set; } = new();

        async Task<int> OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken = default)
        {
            Logger.Log("");
            Logger.AppendLine($"--- Starting sitemap generator. DateTime UTC = {DateTime.UtcNow} ---");
            if (string.IsNullOrEmpty(Url))
            {
                Logger.LogError($"Url was empty");
                app.ShowHelp();
                return 0;
            }

            Uri uri = new(Url.TrimEnd().Trim());
            Logger.Log($"Crawling {uri}");

            await WebCrawler.Crawl(uri, uri, cancellationToken);
            Logger.Log("Crawling complete.");
            Logger.Log("Attempting to generate sitemap.");
            var savedSiteMap = await SiteMap.GenerateSitemapAsync(SiteMapPath, WebCrawler.SitemapEntries, cancellationToken);

            if (savedSiteMap)
            {
                SiteMap.SaveLogs(Logger, LogPath.value);
                return 0;
            }
            Logger.LogError($"There was an error while creating the sitemap for {Url}");
            SiteMap.SaveLogs(Logger, LogPath.value);
            return 1;
        }


    }
}