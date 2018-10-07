using System.Net;

namespace ImageScraper.Plugins.CookieEditor
{
    public class Settings
    {
        public bool Enabled;
        public Cookie Cookie;

        public Settings()
        {
            Enabled = false;
            Cookie = new Cookie();
        }
    }
}
