using System.Net;

namespace ImageScraper.Plugins.SeigaParser
{
    public class Account
    {
        public string Id;
        public string Pass;
        public bool Enabled;
        public CookieContainer CookieContainer;

        public Account()
        {
            Enabled = false;
            CookieContainer = new CookieContainer();
        }

        public Account(string id, string pass)
        {
            Id = id;
            Pass = pass;
            Enabled = false;
            CookieContainer = new CookieContainer();
        }
    }
}
