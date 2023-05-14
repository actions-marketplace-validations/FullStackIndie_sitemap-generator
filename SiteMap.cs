using SiteMapGenerator.Models;
using System.Xml;
using System.Xml.Linq;

namespace SiteMapGenerator
{
    public static class SiteMap
    {
        private static XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        public static async Task<bool> GenerateSitemapAsync(string? siteMapPath, List<SitemapEntry> sitemapEntries, CancellationToken cancellationToken)
        {
            sitemapEntries = RemoveDuplicateUrlEntries(sitemapEntries);

            // Create an XML document for the sitemap
            var xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
                new XElement(xmlns + "urlset", sitemapEntries.Select(ToXElement)));

            string? filteredPath = GetFilteredPath(siteMapPath);
            if (string.IsNullOrEmpty(filteredPath)) { return false; }

            Program.Logger.Log($"Saving sitemap to file {filteredPath}");
            using FileStream fileStream = new(filteredPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
            using XmlWriter xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Async = true, Indent = true });

            await xmlDoc.SaveAsync(xmlWriter, cancellationToken);
            Program.Logger.Log($"Sitemap generated at {filteredPath}", consoleColor: ConsoleColor.Green);
            return true;
        }

        private static XElement ToXElement(SitemapEntry entry)
        {
            return new XElement(xmlns + "url",
                new XElement(xmlns + "loc", entry.Url),
                new XElement(xmlns + "lastmod", entry.LastModified.ToString("yyyy-MM-dd")),
                new XElement(xmlns + "changefreq", entry.ChangeFrequency.ToString().ToLowerInvariant()));
        }

        private static List<SitemapEntry> RemoveDuplicateUrlEntries(List<SitemapEntry> entries)
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

        private static string? GetFilteredPath(string? siteMapPath)
        {
            string? path = null;
            if (string.IsNullOrEmpty(siteMapPath) || siteMapPath == ".")
            {
                path = $"{Directory.GetCurrentDirectory().Trim('/').Replace(Path.DirectorySeparatorChar, '/')}/sitemap.xml";
            }
            else if (Path.Exists(siteMapPath.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/')))
            {
                siteMapPath = siteMapPath.TrimStart('/');
                path = $"{siteMapPath.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/')}/sitemap.xml";
            }
            else
            {
                Program.Logger.LogError("SiteMap Path is invalid. Make directory exists. Exiting");
                return null;
            }

            if (siteMapPath.StartsWith("/C:/") || siteMapPath.Contains("/c:/") || siteMapPath.Contains("/c/"))
            {
                path = path.TrimStart('/');
            }
            else if (!path.StartsWith("C:/"))
            {
                path = $"/{path.TrimStart('/')}";
            }

            Program.Logger.Log($"Path is valid at {path}");
            return path;
        }

    }
}
