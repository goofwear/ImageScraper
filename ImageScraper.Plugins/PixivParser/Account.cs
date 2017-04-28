using System.Net;

namespace ImageScraper.Plugins.PixivParser
{
    public class Account
    {
        public string Id;
        public string Pass;
        public bool Enabled;
        public CookieContainer Cookies;

        public Account()
        {
            Enabled = false;
            Cookies = new CookieContainer();
        }

        public Account(string id, string pass)
        {
            Id = id;
            Pass = pass;
            Enabled = false;
            Cookies = new CookieContainer();
        }
    }
}
