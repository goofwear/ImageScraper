using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace SeigaParsePlugin
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

    public class Settings
    {
        public bool Enabled;
        public string Id;
        public string Pass;
    }

    public class Parser : PluginInterface.PluginInterface
    {
        bool _enabled;
        bool _formEnabled;
        Account _userAccount;
        PluginForm _pluginForm;
        Uri _baseUri = new Uri("http://seiga.nicovideo.jp/");

        public string Name
        {
            get
            {
                return "SeigaParsePlugin";
            }
        }
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
        }
        public bool IsLoggedIn
        {
            get
            {
                return _userAccount.Enabled;
            }
        }

        public Parser()
        {
            _enabled = false;
            _formEnabled = true;
            _userAccount = new Account();
        }

        public void SaveSettings()
        {
            try
            {
                Settings settings = new Settings();
                settings.Id = _userAccount.Id;
                settings.Pass = _userAccount.Pass;
                settings.Enabled = _enabled;
                XmlSerializer xs = new XmlSerializer(typeof(Settings));
                using (StreamWriter sw = new StreamWriter("plugins/" + Name + ".xml", false, new UTF8Encoding(false)))
                    xs.Serialize(sw, settings);
            }
            catch { }
        }

        public void LoadSettings()
        {
            try
            {
                Settings settings = new Settings();
                XmlSerializer xs = new XmlSerializer(typeof(Settings));
                using (StreamReader sr = new StreamReader("plugins/" + Name + ".xml", new UTF8Encoding(false)))
                    settings = (Settings)xs.Deserialize(sr);
                _enabled = settings.Enabled;
                _userAccount.Id = settings.Id;
                _userAccount.Pass = settings.Pass;
            }
            catch { }
        }

        public CookieCollection GetCookieCollection()
        {
            return _userAccount.Cookies.GetCookies(_baseUri);
        }

        public void ShowPluginForm()
        {
            if (_pluginForm == null || _pluginForm.IsDisposed)
            {
                _pluginForm = new PluginForm();
                _pluginForm.Text = Name;
                _pluginForm.MaximizeBox = false;
                _pluginForm.MinimizeBox = false;
                _pluginForm.Host = this;
                _pluginForm.SetAccount(_userAccount);
                _pluginForm.SetEnabled();
                _pluginForm.SetFormEnabled(_formEnabled);
                _pluginForm.Show();
            }
        }

        public void InitializePlugin()
        {
            _formEnabled = false;
            if (_pluginForm != null && !_pluginForm.IsDisposed)
            {
                _userAccount = _pluginForm.GetAccount();
                _enabled = _pluginForm.GetEnabled();
                _pluginForm.SetFormEnabled(_formEnabled);
            }
        }

        public void FinalizePlugin()
        {
            _formEnabled = true;
            if (_pluginForm != null && !_pluginForm.IsDisposed)
            {
                _pluginForm.SetFormEnabled(_formEnabled);
            }
        }

        internal void SetAccount(string id, string pass)
        {
            _userAccount = new Account(id, pass);
        }

        internal void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        public bool Login()
        {
            const string path = "https://secure.nicovideo.jp/secure/login?site=niconico";
            var req = (HttpWebRequest)WebRequest.CreateHttp(new Uri(path));
            var param = String.Format("next_url={0}&mail={1}&password={2}", "", 
                Uri.EscapeDataString(_userAccount.Id), Uri.EscapeDataString(_userAccount.Pass));
            var buf = Encoding.UTF8.GetBytes(param);

            req.Method = "POST";
            req.Proxy = null;
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            req.CookieContainer = new CookieContainer();
            using (var rs = req.GetRequestStream())
            {
                rs.Write(buf, 0, buf.Length);
            }
            var res = req.GetResponse();
            var ccol = req.CookieContainer.GetCookies(_baseUri);
            req.Abort();
            if (ccol["user_session"] != null)
            {
                ccol.Add(new Cookie("accept_fetish_warning", "1", "/", "seiga.nicovideo.jp"));
                _userAccount.Enabled = true;
                _userAccount.Cookies.Add(ccol);
                return true;
            }
            return false;
        }

        internal bool Login(string id, string pass)
        {
            SetAccount(id, pass);
            return Login();
        }

        public bool IsLogoutUrl(string url)
        {
            return new Regex("(http://.+?.nicovideo.jp/logout.*)").Match(url).Success;
        }

        public bool IsParseUrl(string url)
        {
            return new Regex("http://seiga.nicovideo.jp/.+").Match(url).Success;
        }

        //public List<UrlContainer> GetSeigaUrl(HashSet<UrlContainer> urlSet)
        //{
        //    List<UrlContainer> ret = new List<UrlContainer>();
        //    Regex re = new Regex("http://seiga.nicovideo.jp/seiga/im(?<Id>[0-9]+)$");

        //    foreach (var uc in urlSet)
        //    {
        //        Match m = re.Match(uc.Url);
        //        if (m.Success)
        //        {
        //            uc.Url = "http://seiga.nicovideo.jp/image/source?id=" + m.Groups["Id"].Value;
        //            string resUrl = uc.GetResponseUrl(Cookies);
        //            uc.DownloadUrl = resUrl.Replace("/o/", "/priv/");
        //            if (!String.IsNullOrEmpty(uc.DownloadUrl))
        //            {
        //                ret.Add(uc);
        //            }
        //        }
        //    }
        //    return ret;
        //}

        private string GetSeigaDisplayMode(UrlContainer.UrlContainer uc)
        {
            Regex re = new Regex("http://seiga.nicovideo.jp/(?<Mode>seiga|watch)");
            Match m = re.Match(uc.Url);
            if (m.Success)
                return m.Groups["Mode"].Value;
            return null;
        }

        public List<UrlContainer.UrlContainer> GetImageUrlList(UrlContainer.UrlContainer uc, string[] format)
        {
            List<UrlContainer.UrlContainer> ret = new List<UrlContainer.UrlContainer>();
            string mode = GetSeigaDisplayMode(uc);
            if (mode == "seiga")
            {
                Regex re = new Regex("http://seiga.nicovideo.jp/seiga/im(?<Id>[0-9]+)$");
                Match m = re.Match(uc.Url);
                if (m.Success)
                {
                    uc.Url = "http://seiga.nicovideo.jp/image/source?id=" + m.Groups["Id"].Value;
                    string resUrl = uc.GetResponseUrl(_userAccount.Cookies);
                    uc.DownloadUrl = resUrl.Replace("/o/", "/priv/");
                    if (!String.IsNullOrEmpty(uc.DownloadUrl))
                        ret.Add(uc);
                }
            }
            else if (mode == "watch")
            {
                var hc = new HtmlContainer.HtmlContainer(uc, _userAccount.Cookies);
                hc.UpdateAttributeUrlList("img", "data-original", null);
                ret = hc.AttributeUrlList;
            }
            return ret;
        }
    }
}
