using System.Net;

namespace ImageScraper.Plugins.CookieEditor
{
    public class SerializableCookie
    {
        public string Name;
        public string Value;
        public string Path;
        public string Domain;

        public SerializableCookie()
        {
            Name = "";
            Value = "";
            Path = "";
            Domain = "";
        }

        public SerializableCookie(string name, string value, string path, string domain)
        {
            Name = name; 
            Value = value; 
            Path = path;
            Domain = domain;
        }
    }

    public class Settings
    {
        public bool Enabled;
        public SerializableCookie Cookie;

        public Settings()
        {
            Enabled = false;
            Cookie = new SerializableCookie();
        }

        public Cookie GetCookie()
        {
            if (Cookie.Name == "")
                return new Cookie();
            else
                return new Cookie("", Cookie.Value, Cookie.Path, Cookie.Domain);
        }
    }
}
