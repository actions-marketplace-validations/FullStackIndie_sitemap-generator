using HtmlAgilityPack;
using SiteMapGenerator.Data.Enums;
using SiteMapGenerator.Models;
using System.Xml;
using System.Xml.Linq;

namespace SiteMapGenerator
{
    public class WebCrawler
    {
        private XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        private List<SitemapEntry> sitemapEntries;
        private HashSet<string> invalidPaths = new HashSet<string>()
        {
            "/", "mailto:", "//"
        };

        public WebCrawler()
        {
            sitemapEntries = new List<SitemapEntry>();
        }

        public async Task Crawl(Uri url, Uri parentUrl, CancellationToken cancellationToken)
        {
            try
            {
                var htmlWeb = new HtmlWeb();
                var htmlDocument = await htmlWeb.LoadFromWebAsync(url.ToString(), cancellationToken);

                // Parse the HTML document and extract relevant information
                // For example, extract links and metadata for the sitemap
                var extractedLinks = ExtractLinks(htmlDocument).Distinct();

                // Add the sitemap entry to the list
                sitemapEntries.Add(new SitemapEntry
                {
                    Url = url.ToString(),
                    LastModified = DateTime.Now,
                    ChangeFrequency = ChangeFrequency.Daily
                });

                // Recursively crawl the extracted links
                foreach (var link in extractedLinks)
                {
                    if (invalidPaths.Any(i => link.StartsWith(i)) && !link.StartsWith("//"))
                    {
                        Program.Logger.Log($"Skipping link '{link}' because it isnt a valid url", ConsoleColor.Yellow);
                        continue;
                    }
                    if (link.StartsWith("#"))
                    {
                        Program.Logger.Log($"Skipping link '{link}' because it is a fragment url", ConsoleColor.Yellow);
                        continue;
                    }
                    if (link.Split('.').Any(p => parentUrl.Host.Contains(p)) && link != parentUrl.Host)
                    {
                        Program.Logger.Log($"Skipping link '{link}' because it is a link for a different sub-domain", ConsoleColor.Yellow);
                        continue;
                    }
                    if (!link.Contains(parentUrl.Host))
                    {
                        Program.Logger.Log($"Skipping link '{link}' because it is a link for a different domain", ConsoleColor.Yellow);
                        continue;
                    }
                    if (link.StartsWith("//"))
                    {
                        var newLink = $"{parentUrl.Scheme}:{link}".TrimEnd('/');

                        Program.Logger.Log($"Crawling {newLink}");
                        await Crawl(new Uri(newLink), parentUrl, cancellationToken);
                        continue;
                    }

                    Program.Logger.Log($"Crawling {link.TrimEnd('/')}");
                    await Crawl(new Uri(link.TrimEnd('/')), parentUrl, cancellationToken);
                }
            }
            catch (HttpRequestException httpEx)
            {
                Program.Logger.LogError($"Error crawling {url} - {httpEx.Message}. \n Make sure host name (url) is correct");
            }
            catch (Exception ex)
            {
                Program.Logger.LogError($"Error crawling {url} - {ex.Message} \n ---- [ StackTrace ] ----- \n {ex.StackTrace}");
            }

        }

        private List<string> ExtractLinks(HtmlDocument htmlDocument)
        {
            var links = new List<string>();

            // Use HtmlAgilityPack to select <a> elements and extract the "href" attribute
            var anchorNodes = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
            if (anchorNodes != null)
            {
                foreach (var attribute in anchorNodes.Select(a => a.Attributes))
                {
                    var hrefAttribute = attribute["href"];
                    var dataHrefAttribute = attribute["data-href"];
                    if (hrefAttribute != null)
                    {
                        var link = hrefAttribute.Value;
                        links.Add(link);
                    }
                    if (dataHrefAttribute != null)
                    {
                        var link = dataHrefAttribute.Value;
                        links.Add(link);
                    }
                }
            }

            return links;
        }



        public async Task<bool> GenerateSitemapAsync(string? siteMapPath, CancellationToken cancellationToken)
        {
            sitemapEntries = RemoveDuplicateUrlEntries(sitemapEntries);

            // Create an XML document for the sitemap
            var xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
                new XElement(xmlns + "urlset", sitemapEntries.Select(ToXElement)));

            string? path = null;
            if (string.IsNullOrEmpty(siteMapPath) || siteMapPath == ".")
            {
                path = $"{Directory.GetCurrentDirectory().Trim('/').Replace(Path.DirectorySeparatorChar, '/')}/sitemap.xml";
                Program.Logger.Log($"Path is valid at {path}");
            }
            else if (Path.Exists(siteMapPath.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/')))
            {
                path = $"{siteMapPath.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/')}/sitemap.xml";
                Program.Logger.Log($"Path is valid at {path}");
            }
            else
            {
                Program.Logger.LogError("SiteMap Path is invalid. Make directory exists. Exiting");
                return false;
            }

            Program.Logger.Log($"Saving sitemap to file {path}");
            using FileStream fileStream = new(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
            using XmlWriter xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Async = true, Indent = true });

            await xmlDoc.SaveAsync(xmlWriter, cancellationToken);
            Program.Logger.Log($"Sitemap generated at {path}", consoleColor: ConsoleColor.Green);
            return true;
        }

        private XElement ToXElement(SitemapEntry entry)
        {
            return new XElement(xmlns + "url",
                new XElement(xmlns + "loc", entry.Url),
                new XElement(xmlns + "lastmod", entry.LastModified.ToString("yyyy-MM-dd")),
                new XElement(xmlns + "changefreq", entry.ChangeFrequency.ToString().ToLowerInvariant()));
        }

        private List<SitemapEntry> RemoveDuplicateUrlEntries(List<SitemapEntry> entries)
        {
            var result = new List<SitemapEntry>();
            foreach (var entry in entries)
            {
                if (!result.Any(r => r.Url == entry.Url))
                {
                    result.Add(entry);
                }
            }
            return result;
        }
    }

}
