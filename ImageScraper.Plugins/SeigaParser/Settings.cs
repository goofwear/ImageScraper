namespace ImageScraper.Plugins.SeigaParser
{
    public class Settings
    {
        public bool Enabled;
        public string Id;
        public string Pass;
        public bool IsLoggedIn;
        public string Cookies;

        public Settings()
        {
            Enabled = false;
            Id = "";
            Pass = "";
            IsLoggedIn = false;
            Cookies = "";
        }
    }
}
