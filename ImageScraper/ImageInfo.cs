using System;

namespace ImageScraper
{
    public class ImageInfo : IEquatable<ImageInfo>
    {
        public string ImageUrl;
        public string ParentUrl;
        public string ParentTitle;
        public string ImagePath;
        public DateTime LoadDate;

        public ImageInfo()
        {
            ImageUrl = "";
            ParentUrl = "";
            ParentTitle = "";
            ImagePath = "";
            LoadDate = DateTime.Now;
        }

        public override int GetHashCode()
        {
            return this.ImageUrl.GetHashCode();
        }

        bool IEquatable<ImageInfo>.Equals(ImageInfo imageInfo)
        {
            if (imageInfo == null)
                return false;
            return (this.ImageUrl == imageInfo.ImageUrl);
        }
    }
}
