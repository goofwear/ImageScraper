using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageScraper
{
    public class FilterResolution
    {
        FilterValueRange widthFilter;
        FilterValueRange heightFilter;

        public FilterResolution(bool en_min, bool en_max, int w_min, int h_min, int w_max, int h_max)
        {
            this.widthFilter = new FilterValueRange(en_min, en_max, w_min, w_max);
            this.heightFilter = new FilterValueRange(en_min, en_max, h_min, h_max);
        }

        public bool Filter(int w, int h)
        {
            return this.widthFilter.Filter(w) || this.heightFilter.Filter(h);
        }
    }

    public class CheckTerminated
    {
        bool enabledDepth;
        bool enabledLinkCount;
        bool enabledImageCount;
        bool enabledSize;
        bool enabledDirImageCount;
        Status lim;
        int limDirImageCount;
        int currentDirImageCount;

        public CheckTerminated(bool[] enabled, Status lim, int lim_dic, int cur_dic)
        {
            this.enabledDepth = enabled[0];
            this.enabledLinkCount = enabled[1];
            this.enabledImageCount = enabled[2];
            this.enabledSize = enabled[3];
            this.enabledDirImageCount = enabled[4];
            this.lim = lim;
            this.limDirImageCount = lim_dic;
            this.currentDirImageCount = cur_dic;
        }

        public bool Check(Status cur)
        {
            if (this.enabledDepth)
            {
                if (cur.depthCount >= this.lim.depthCount)
                    return true;
            }
            else if (this.enabledLinkCount)
            {
                if (cur.pageCount >= this.lim.pageCount)
                    return true;
            }
            else if (this.enabledImageCount)
            {
                if (cur.imageCount >= this.lim.imageCount)
                    return true;
            }
            else if (this.enabledSize)
            {
                if (cur.size >= this.lim.size)
                    return true;
            }
            else if (this.enabledDirImageCount)
            {
                if (this.currentDirImageCount + cur.imageCount >= this.limDirImageCount)
                    return true;
            }
            return false;
        }
    }

    public class FilterColorFormat
    {
        bool enabledGrey;
        bool enabledColor;

        public FilterColorFormat(bool en_color, bool en_grey)
        {
            this.enabledGrey = en_grey;
            this.enabledColor = en_color;
        }

        private float IsGreyscale(Image img)
        {
            int imgWidth = img.Width;
            int imgHeight = img.Height;
            var subTotals = new float[imgWidth * imgHeight];
            var rect = new Rectangle(0, 0, imgWidth, imgHeight);

            using (Bitmap newBmp = new Bitmap(img))
            using (Bitmap targetBmp = newBmp.Clone(rect, PixelFormat.Format24bppRgb))
            {
                unsafe
                {
                    var bitmapData = targetBmp.LockBits(rect, ImageLockMode.ReadWrite, targetBmp.PixelFormat);
                    int heightInPixels = imgHeight;
                    int widthInBytes = imgWidth * 3;
                    byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                    Parallel.For(0, heightInPixels, y =>
                    {
                        byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                        for (int x = 0; x < imgWidth; x++)
                        {
                            int xPor3 = x * 3;
                            float b = currentLine[xPor3++];
                            float g = currentLine[xPor3++];
                            float r = currentLine[xPor3];

                            subTotals[y * imgWidth + x] = (float)(Math.Pow(r - b, 2) + Math.Pow(r - g, 2));
                        }
                    });
                    targetBmp.UnlockBits(bitmapData);
                }
                return subTotals.Sum() / (imgWidth * imgHeight);
            }
        }

        public bool Filter(Image img)
        {
            if (!this.enabledGrey)
                return IsGreyscale(img) < 1;
            if (!this.enabledColor)
                return IsGreyscale(img) > 1;
            return false;
        }
    }

    public class FilterDomain
    {
        UrlContainer.UrlContainer baseUrl;
        bool enabledDomain;
        string authority;
        string localPath;

        public FilterDomain(UrlContainer.UrlContainer uc, bool en_d)
        {
            this.baseUrl = uc;
            this.enabledDomain = en_d;
            this.authority = this.baseUrl.Authority;
            this.localPath = this.baseUrl.LocalPath;
        }

        public List<UrlContainer.UrlContainer> Filter(List<UrlContainer.UrlContainer> urlList)
        {
            var newLinkList = new List<UrlContainer.UrlContainer>();
            foreach (UrlContainer.UrlContainer url in urlList)
            {
                if (newLinkList.Where(x => x.RawUrl == url.RawUrl).Count() == 0)
                {
                    // ドメインが異なりかつ検索設定が無効なら
                    if (this.authority == url.Authority || this.enabledDomain)
                        newLinkList.Add(url);
                }
            }
            return newLinkList;
        }
    }

    public class FilterKeyword
    {
        bool mEnabled;
        bool mContains;
        bool mNotContains;
        string[] mKeywords;
        string[] mNGKeywords;

        public FilterKeyword(bool enabled, bool mContains, bool mNotContains, string keywords, string NGKeywords)
        {
            this.mEnabled = enabled;
            this.mContains = mContains;
            this.mNotContains = mNotContains;
            this.mKeywords = keywords.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            this.mNGKeywords = NGKeywords.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        public bool Filter(string key)
        {
            bool flag = false;
            if (this.mEnabled && !String.IsNullOrEmpty(key))
            {
                if (mContains)
                    flag = (mKeywords.Where(x => key.Contains(x)).Count() == 0);
                if (mNotContains)
                    flag = (mNGKeywords.Where(x => key.Contains(x)).Count() > 0);
            }
            
            return flag;
        }
    }

    public class FilterValueRange
    {
        int min;
        int max;
        bool enabledMin;
        bool enabledMax;

        public FilterValueRange(bool en_min, bool en_max, int min, int max)
        {
            this.min = min;
            this.max = max;
            this.enabledMin = en_min;
            this.enabledMax = en_max;
        }

        public bool Filter(int val)
        {
            if (this.enabledMin && val < min)
                return true;
            if (this.enabledMax && val > max)
                return true;
            return false;
        }
    }

    public class FilterUrlOverlapped
    {
        public bool Enabled { get; set; }
        HashSet<string> _cachedUrlSet;

        public FilterUrlOverlapped(HashSet<ImageInfo> urlTable)
        {
            this._cachedUrlSet = new HashSet<string>(urlTable.Select(x => x.ImageUrl));
        }

        public void Clear()
        {
            this._cachedUrlSet.Clear();
        }

        public void Add(ImageInfo info)
        {
            this._cachedUrlSet.Add(info.ImageUrl);
        }

        public List<UrlContainer.UrlContainer> Filter(List<UrlContainer.UrlContainer> urlList)
        {
            if (this.Enabled)
                return urlList;

            var ret = new List<UrlContainer.UrlContainer>();
            foreach (var uc in urlList)
            {
                if (!_cachedUrlSet.Contains(uc.Url))
                    ret.Add(uc);
            }
            return ret;
        }
    }

    public class FileNameGenerator
    {
        bool enabledSerial;
        SerialNameGenerator serialNameGen;

        public FileNameGenerator(bool en_sng, SerialNameGenerator sng)
        {
            serialNameGen = sng;
            enabledSerial = en_sng;
        }

        public string Generate(string dir, string oldName)
        {
            if (enabledSerial)
            {
                string ext = Path.GetExtension(oldName);
                return serialNameGen.Generate(dir) + ext;
            }
            else
                return oldName;
        }
    }
}
