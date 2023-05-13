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
            "/", "mailto:"
        };

        public WebCrawler()
        {
            sitemapEntries = new List<SitemapEntry>();
        }

        public async Task Crawl(Uri url, Uri parentUrl, CancellationToken cancellationToken)
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
                if (invalidPaths.Any(i => link.StartsWith(i)))
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

                Program.Logger.Log($"Crawling {link.TrimEnd('/')}");
                await Crawl(new Uri(link.TrimEnd('/')), parentUrl, cancellationToken);
            }
        }

        private List<string> ExtractLinks(HtmlDocument htmlDocument)
        {
            var links = new List<string>();

            // Use HtmlAgilityPack to select <a> elements and extract the "href" attribute
            var anchorNodes = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
            if (anchorNodes != null)
            {
                foreach (var anchorNode in anchorNodes)
                {
                    var hrefAttribute = anchorNode.Attributes["href"];
                    if (hrefAttribute != null)
                    {
                        var link = hrefAttribute.Value;
                        // Filter and normalize the link if needed
                        // Add the link to the list of extracted links
                        links.Add(link);
                    }
                }
            }

            return links;
        }



        public async Task GenerateSitemapAsync(CancellationToken cancellationToken)
        {
            sitemapEntries = RemoveDuplicateUrlEntries(sitemapEntries);
            // Create an XML document for the sitemap
            var xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
                new XElement(xmlns + "urlset", sitemapEntries.Select(ToXElement)));
            Program.Logger.Log("Saving sitemap to file...");
            // Save the XML document to a file
            using FileStream fileStream = new("sitemap.xml", FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
            using XmlWriter xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Async = true, Indent = true });
            await xmlDoc.SaveAsync(xmlWriter, cancellationToken);
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
            var urls = new HashSet<string>(entries.Select(e => e.Url));
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
