using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace ImageScraper.Plugins.CookieEditor
{
    public class Editor : Plugins.IPlugin
    {
        Utilities.Logger mLogger;
        PluginForm mPluginForm;
        Settings mSettings;

        public string Name
        {
            get { return "CookieEditor"; }
        }

        public bool Enabled { get; private set; }

        public bool IsLoggedIn { get; private set; }

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
            Enabled = false;
            mSettings = new Settings();
            IsLoggedIn = false;
            mLogger = null;
        }

        public void SaveSettings()
        {
            mSettings.Enabled = Enabled;
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
            if (!String.IsNullOrEmpty(mSettings.Cookie.Name))
                cc.Add(mSettings.Cookie);
            return cc;
        }

        public void ShowPluginForm()
        {
            if (mPluginForm == null || mPluginForm.IsDisposed)
            {
                mPluginForm = new PluginForm(this);
                mPluginForm.Text = Name;
                mPluginForm.SetCookie(mSettings.Cookie);
                mPluginForm.MaximizeBox = false;
                mPluginForm.MinimizeBox = false;
                mPluginForm.Show();
            }
        }

        internal void SetCookie(Cookie cookie)
        {
            mSettings.Cookie = cookie;
        }

        public void PreProcess()
        {
            // フォームが開かれているとき実行されアカウント情報が反映される
            if (mPluginForm != null && !mPluginForm.IsDisposed)
            {
                Enabled = mPluginForm.GetEnabled();
                if (Enabled)
                    SetCookie(mPluginForm.GetCookie());
                mPluginForm.SetFormEnabled(false);
            }
        }

        public void PostProcess()
        {
            if (mPluginForm != null && !mPluginForm.IsDisposed)
                mPluginForm.SetFormEnabled(true);
        }

        public bool Login(bool force = false)
        {
            IsLoggedIn = true;
            return IsLoggedIn;
        }

        public bool IsIgnore(string url)
        {
            return false;
        }

        public bool IsParse(string url)
        {
            return true;
        }

        public List<UrlContainer.UrlContainer> GetLinkList(HtmlContainer.HtmlContainer hc)
        {
            return new List<UrlContainer.UrlContainer>();
        }

        public List<UrlContainer.UrlContainer> GetImageUrlList(UrlContainer.UrlContainer uc, string[] format)
        {
            return new List<UrlContainer.UrlContainer>();
        }
    }
}
