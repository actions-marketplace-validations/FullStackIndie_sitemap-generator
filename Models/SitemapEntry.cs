using SiteMapGenerator.Data.Enums;

namespace SiteMapGenerator.Models
{
    public class SitemapEntry
    {
        public string Url { get; set; }
        public DateTime LastModified { get; set; }
        public ChangeFrequency ChangeFrequency { get; set; }
    }
}
