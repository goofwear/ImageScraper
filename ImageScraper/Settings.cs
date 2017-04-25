using System;
using System.Net;
using System.Collections.Generic;
using Utilities;

namespace ImageScraper
{
    public class FormSettings
    {
        public List<string> UrlList;
        public List<string> TitleCKeywordList;
        public List<string> TitleNCKeywordList;
        public List<string> UrlCKeywordList;
        public List<string> UrlNCKeywordList;
        public ControlProperty.Property[] Properties;

        public FormSettings()
        {
            UrlList = new List<string>();
            TitleCKeywordList = new List<string>();
            TitleNCKeywordList = new List<string>();
            UrlCKeywordList = new List<string>();
            UrlNCKeywordList = new List<string>();
            Properties = new ControlProperty.Property[0];
        }
    }

    public class DownloadSettings
    {
        public UrlContainer.UrlContainer urlContainer;
        public string[] format;
        public bool enabledHref;
        public bool enabledIsrc;
        public FilterValueRange filterImageSize;
        public FilterValueRange filterImageCount;
        public FilterResolution filterResolution;
        public FilterColorFormat filterColorFormat;
        public FilterDomain filterDomain;
        public FilterKeyword filterTitle;
        public FilterKeyword filterUrl;
        public PluginInterface[] plugins;
        public CookieContainer cookies;
        public string dest;
        public bool destPlusUrl;
        public bool destPlusTitle;
        public FilterUrlOverlapped filterUrlOverlapped;
        public FileNameGenerator fileNameGenerator;
        public CheckTerminated checkTerminated;
    }

    public class Status
    {
        public int depthCount;
        public int pageCount;
        public int imageCount;
        public double size;

        public Status(int depth, int page, int image, double size)
        {
            this.depthCount = depth;
            this.pageCount = page;
            this.imageCount = image;
            this.size = size;
        }
    }

    public class ImageInfo
    {
        public string ParentUrl;
        public string ParentTitle;
        public string ImagePath;
        public DateTime LoadDate;

        public ImageInfo()
        {
            ParentUrl = "";
            ParentTitle = "";
            ImagePath = "";
            LoadDate = DateTime.Now;
        }
    }
}
