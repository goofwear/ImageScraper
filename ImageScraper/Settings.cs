using System.Collections.Generic;

namespace ImageScraper
{
    public class FormSettings
    {
        public List<string> UrlList;
        public List<string> KeyTitleList;
        public List<string> ExKeyTitleList;
        public List<string> KeyUrlList;
        public List<string> ExKeyUrlList;
        public ControlProperty.Property[] Properties;

        public FormSettings()
        {
            UrlList = new List<string>();
            KeyTitleList = new List<string>();
            ExKeyTitleList = new List<string>();
            KeyUrlList = new List<string>();
            ExKeyUrlList = new List<string>();
            Properties = new ControlProperty.Property[0];
        }
    }

    public class DownloadSettings
    {
        public UrlContainer.UrlContainer UrlContainer;
        public string[] Formats;
        public bool ParseHrefAttr;
        public bool ParseImgTag;
        public ValueRangeFilter ImageSizeFilter;
        public ValueRangeFilter ImagesPerPageFilter;
        public ResolutionFilter ResolutionFilter;
        public ColorFilter ColorFilter;
        public DomainFilter DomainFilter;
        public KeywordFilter TitleFilter;
        public KeywordFilter UrlFilter;
        public string RootDirectory;
        public bool AppendsUrl;
        public bool AppendsTitle;
        public OverlappedUrlFilter OverlappedUrlFilter;
        public FileNameGenerator FileNameGenerator;
        public StatusMonitor StatusMonitor;
    }

    public class Status
    {
        public int Depth;
        public int Pages;
        public int Images;
        public double Size;

        public Status(int depth, int page, int image, double size)
        {
            this.Depth = depth;
            this.Pages = page;
            this.Images = image;
            this.Size = size;
        }
    }
}
