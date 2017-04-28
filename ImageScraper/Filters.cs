using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageScraper
{
    public class ResolutionFilter
    {
        ValueRangeFilter mWidthFilter;
        ValueRangeFilter mHeightFilter;

        public ResolutionFilter(bool enMin, bool enMax, int wMin, int hMin, int wMax, int hMax)
        {
            mWidthFilter = new ValueRangeFilter(enMin, enMax, wMin, wMax);
            mHeightFilter = new ValueRangeFilter(enMin, enMax, hMin, hMax);
        }

        public bool Filter(Bitmap bitmap)
        {
            return mWidthFilter.Filter(bitmap.Width) || mHeightFilter.Filter(bitmap.Height);
        }
    }

    public class StatusMonitor
    {
        bool mDepthEnabled;
        bool mLinksEnabled;
        bool mImagesEnabled;
        bool mSizeEnabled;
        bool mExistImagesEnabled;
        Status mLimit;
        int mLimitExistImages;
        int mExistImages;

        public StatusMonitor(bool[] enabled, Status limit, int limExists, int exists)
        {
            mDepthEnabled = enabled[0];
            mLinksEnabled = enabled[1];
            mImagesEnabled = enabled[2];
            mSizeEnabled = enabled[3];
            mExistImagesEnabled = enabled[4];
            mLimit = limit;
            mLimitExistImages = limExists;
            mExistImages = exists;
        }

        public bool HasCompleted(Status currentStatus)
        {
            if (mDepthEnabled)
            {
                if (currentStatus.Depth >= mLimit.Depth)
                    return true;
            }
            else if (mLinksEnabled)
            {
                if (currentStatus.Pages >= mLimit.Pages)
                    return true;
            }
            else if (mImagesEnabled)
            {
                if (currentStatus.Images >= mLimit.Images)
                    return true;
            }
            else if (mSizeEnabled)
            {
                if (currentStatus.Size >= mLimit.Size)
                    return true;
            }
            else if (mExistImagesEnabled)
            {
                if (mExistImages + currentStatus.Images >= mLimitExistImages)
                    return true;
            }
            return false;
        }
    }

    public class ColorFilter
    {
        bool mGreyEnabled;
        bool mColorEnabled;

        public ColorFilter(bool enColor, bool enGrey)
        {
            this.mGreyEnabled = enGrey;
            this.mColorEnabled = enColor;
        }

        private static bool IsGreyscale(Image img, float threshold)
        {
            int imgWidth = img.Width;
            int imgHeight = img.Height;
            var subTotals = new float[imgWidth * imgHeight];
            var rect = new Rectangle(0, 0, imgWidth, imgHeight);

            using (var targetBmp = new Bitmap(img).Clone(rect, PixelFormat.Format24bppRgb))
            {
                unsafe
                {
                    var bitmapData = targetBmp.LockBits(rect, ImageLockMode.ReadWrite, targetBmp.PixelFormat);
                    byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                    Parallel.For(0, imgHeight, y =>
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
                return subTotals.Sum() / (imgWidth * imgHeight) < threshold;
            }
        }

        public bool Filter(Image img)
        {
            if (!this.mGreyEnabled)
                return IsGreyscale(img, 1.0f);
            if (!this.mColorEnabled)
                return !IsGreyscale(img, 1.0f);
            return false;
        }
    }

    public class DomainFilter
    {
        bool mEnabled;
        UrlContainer.UrlContainer mBaseUrl;

        public DomainFilter(bool enabled, UrlContainer.UrlContainer uc)
        {
            mEnabled = enabled;
            this.mBaseUrl = uc;
        }

        public List<UrlContainer.UrlContainer> Filter(List<UrlContainer.UrlContainer> urlList)
        {
            var filteredList = new List<UrlContainer.UrlContainer>();
            foreach (UrlContainer.UrlContainer url in urlList)
            {
                if (filteredList.Where(x => x.RawUrl == url.RawUrl).Count() == 0)
                {
                    // ドメインが異なりかつ検索設定が無効なら
                    if (this.mBaseUrl.Authority == url.Authority || this.mEnabled)
                        filteredList.Add(url);
                }
            }
            return filteredList;
        }
    }

    public class KeywordFilter
    {
        bool mEnabled;
        bool mInEnabled;
        bool mExEnabled;
        string[] mKeywords;
        string[] mExKeywords;

        public KeywordFilter(bool enabled, bool enIn, bool enEx, string inKeys, string exKeys)
        {
            this.mEnabled = enabled;
            this.mInEnabled = enIn;
            this.mExEnabled = enEx;
            this.mKeywords = inKeys.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            this.mExKeywords = exKeys.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        public bool Filter(string key)
        {
            bool flag = false;
            if (this.mEnabled && !String.IsNullOrEmpty(key))
            {
                if (mInEnabled)
                    flag = (mKeywords.Where(x => key.Contains(x)).Count() == 0);
                if (mExEnabled)
                    flag = (mExKeywords.Where(x => key.Contains(x)).Count() > 0);
            }
            
            return flag;
        }
    }

    public class ValueRangeFilter
    {
        int mMinimum;
        int mMaximum;
        bool mMinimumEnabled;
        bool mMaximumEnabled;

        public ValueRangeFilter(bool enMin, bool enMax, int min, int max)
        {
            this.mMinimum = min;
            this.mMaximum = max;
            this.mMinimumEnabled = enMin;
            this.mMaximumEnabled = enMax;
        }

        public bool Filter(int val)
        {
            if (this.mMinimumEnabled && val < mMinimum)
                return true;
            if (this.mMaximumEnabled && val > mMaximum)
                return true;
            return false;
        }
    }

    public class OverlappedUrlFilter
    {
        bool mEnabled { get; set; }
        HashSet<string> mUrlCache;

        public OverlappedUrlFilter(HashSet<ImageInfo> urlTable, bool enabled)
        {
            this.mUrlCache = new HashSet<string>(urlTable.Select(x => x.ImageUrl));
            this.mEnabled = enabled;
        }

        public void Clear()
        {
            this.mUrlCache.Clear();
        }

        public void Add(ImageInfo info)
        {
            this.mUrlCache.Add(info.ImageUrl);
        }

        public List<UrlContainer.UrlContainer> Filter(List<UrlContainer.UrlContainer> urlList)
        {
            if (this.mEnabled)
                return urlList;

            var ret = new List<UrlContainer.UrlContainer>();
            foreach (var uc in urlList)
            {
                if (!mUrlCache.Contains(uc.Url))
                    ret.Add(uc);
            }
            return ret;
        }
    }

    public class FileNameGenerator
    {
        bool mEnabled;
        Utilities.SerialNameGenerator mSerialNameGen;

        public FileNameGenerator(bool enabled, Utilities.SerialNameGenerator sng)
        {
            mEnabled = enabled;
            mSerialNameGen = sng;
        }

        public string Generate(string dir, string oldName)
        {
            if (mEnabled)
            {
                string ext = Path.GetExtension(oldName);
                return mSerialNameGen.Generate(dir) + ext;
            }
            else
                return oldName;
        }
    }
}
