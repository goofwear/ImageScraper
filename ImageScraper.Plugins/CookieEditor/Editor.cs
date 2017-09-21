using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace ImageScraper.Plugins.CookieEditor
{
    public class Editor : Plugins.PluginInterface
    {
        bool mIsLoggedIn;
        Utilities.Logger mLogger;
        PluginForm mPluginForm;
        Settings mSettings;

        public string Name
        {
            get { return "CookieEditor"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public bool IsLoggedIn
        {
            get
            {
                return mIsLoggedIn;
            }
        }

        public bool IsExclusive
        {
            get { return false; }
        }

        public Utilities.Logger Logger
        {
            set { mLogger = value; }
        }

        public Editor()
        {
            mSettings = new Settings();
            mIsLoggedIn = false;
            mLogger = null;
        }

        public void SaveSettings()
        {
            XmlSerializer xs = new XmlSerializer(typeof(Settings));
            using (StreamWriter sw = new StreamWriter("plugins/" + Name + ".xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, mSettings);
        }

        public void LoadSettings()
        {
            XmlSerializer xs = new XmlSerializer(typeof(Settings));
            using (StreamReader sr = new StreamReader("plugins/" + Name + ".xml", new UTF8Encoding(false)))
                mSettings = (Settings)xs.Deserialize(sr);
        }

        public CookieCollection GetCookieCollection()
        {
            var cc = new CookieCollection();
            cc.Add(new Cookie(mSettings.Name, mSettings.Value, mSettings.Path, mSettings.Domain));
            return cc;
        }

        public void ShowPluginForm()
        {
            if (mPluginForm == null || mPluginForm.IsDisposed)
            {
                mPluginForm = new PluginForm(this);
                mPluginForm.Text = Name;
                mPluginForm.SetCookie(mSettings.Name, mSettings.Value, mSettings.Path, mSettings.Domain);
                mPluginForm.MaximizeBox = false;
                mPluginForm.MinimizeBox = false;
                mPluginForm.Show();
            }
        }

        internal void SetCookie(string name, string value, string path, string domain)
        {
            if (mSettings == null)
                mSettings = new Settings();
            mSettings.Name = name;
            mSettings.Value = value;
            mSettings.Path = path;
            mSettings.Domain = domain;
        }

        public void PreProcess()
        {
        }

        public void PostProcess()
        {
        }

        public bool Login(bool force = false)
        {
            mIsLoggedIn = true;
            return mIsLoggedIn;
        }

        public bool IsIgnore(string url)
        {
            return false;
        }

        public bool IsParse(string url)
        {
            return true;
        }

        public List<UrlContainer.UrlContainer> GetImageUrlList(UrlContainer.UrlContainer uc, string[] format)
        {
            return new List<UrlContainer.UrlContainer>();
        }
    }
}
