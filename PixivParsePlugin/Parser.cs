using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using Utilities;

namespace PixivParsePlugin
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

    public class Parser : PluginInterface
    {
        bool _enabled;
        Account _userAccount;
        PluginForm _pluginForm;
        LoggerDelegate _loggerDelegate;
        Uri _baseUri = new Uri("http://www.pixiv.net/");

        public string Name
        {
            get { return "PixivParsePlugin"; }
        }

        public bool Enabled
        {
            get { return _enabled; }
        }

        public bool IsLoggedIn
        {
            get
            {
                var cookie = GetCookieCollection()["device_token"];
                if (cookie != null && DateTime.Now > cookie.Expires)
                    return false;
                else
                    return _userAccount.Enabled;
            }
        }

        public Parser()
        {
            _enabled = false;
            _userAccount = new Account();
            _loggerDelegate = null;
        }

        public void SetLoggerDelegate(LoggerDelegate loggerDelegate)
        {
            _loggerDelegate = loggerDelegate;
        }

        public void SaveSettings()
        {
            try
            {
                Settings settings = new Settings();
                settings.Id = _userAccount.Id;
                settings.Pass = _userAccount.Pass;
                settings.Enabled = _enabled;
                settings.IsLoggedIn = _userAccount.Enabled;
                if (_userAccount.Enabled)
                    settings.Cookies = Common.CookiesToString(GetCookieCollection());
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
                _userAccount.Enabled = settings.IsLoggedIn;
                _loggerDelegate.Write(Name, "プラグインの設定を読み込みました");
                if (settings.IsLoggedIn)
                {
                    var ccol = Common.StringToCookies(settings.Cookies);
                    _userAccount.Cookies.Add(ccol);
                }
            }
            catch
            {
                _loggerDelegate.Write(Name, "設定の読み込みに失敗しました");
            }
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
                _pluginForm.Show();
            }
        }

        public void InitializePlugin()
        {
            // フォームが開かれているとき実行されアカウント情報が反映される
            if (_pluginForm != null && !_pluginForm.IsDisposed)
            {
                _enabled = _pluginForm.GetEnabled();
                if (_enabled)
                {
                    var userAccount = _pluginForm.GetAccount();
                    SetAccount(userAccount.Id, userAccount.Pass);
                }
                _pluginForm.SetFormEnabled(false);
            }
            // 設定を読み込んだあるいはフォームを閉じたときすでにアカウント情報が反映されている
        }

        public void FinalizePlugin()
        {
            if (_pluginForm != null && !_pluginForm.IsDisposed)
                _pluginForm.SetFormEnabled(true);
        }

        internal void SetAccount(string id, string pass)
        {
            if (id != _userAccount.Id || pass != _userAccount.Pass)
                _userAccount = new Account(id, pass);
        }

        internal void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        private string GetInitConfig(string key)
        {
            var hc = new HtmlContainer.HtmlContainer("https://accounts.pixiv.net/login");
            var jsonString = hc.GetAttribute("input", "value", new Dictionary<string, string>() { { "id", "init-config" } });
            string pat = String.Format(@"{0}.:.(?<Key>[a-f0-9]+).", key);
            Match m = new Regex(pat).Match(jsonString);
            if (m.Success)
            {
                _userAccount.Cookies.Add(hc.Cookies.GetCookies(_baseUri));
                return m.Groups["Key"].Value;
            }
            else
                return null;
        }

        public bool Login(bool force = false)
        {
            if (IsLoggedIn && !force)
                return true;

            var param = "";
            var content = new Dictionary<string, string>()
            {
                { "pixiv_id", Uri.EscapeDataString(_userAccount.Id) },
                { "password", Uri.EscapeDataString(_userAccount.Pass) },
                { "post_key", GetInitConfig("pixivAccount.postKey") },
                { "return_to", Uri.EscapeDataString(_baseUri.OriginalString) }
            };
            foreach (var pair in content)
                param += String.Format("{0}={1}&", pair.Key, pair.Value);

            var buf = Encoding.UTF8.GetBytes(param);
            var path = "https://accounts.pixiv.net/api/login?lang=jp";
            var req = (HttpWebRequest)WebRequest.CreateHttp(new Uri(path));
            req.Method = "POST";
            req.Proxy = null;
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            req.CookieContainer = _userAccount.Cookies;
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            using (var rs = req.GetRequestStream())
            {
                rs.Write(buf, 0, buf.Length);
            }
            var res = req.GetResponse();
            var ccol = req.CookieContainer.GetCookies(_baseUri);
            req.Abort();

            if (ccol["device_token"] != null)
            {
                _userAccount.Enabled = true;
                _userAccount.Cookies.Add(ccol);
                _loggerDelegate.Write(Name, "ログインに成功しました");
                return true;
            }
            _loggerDelegate.Write(Name, "ログインに失敗しました");
            return false;
        }

        internal bool Login(string id, string pass)
        {
            SetAccount(id, pass);
            return Login(true);
        }

        public bool IsLogoutUrl(string url)
        {
            return new Regex("https?://www.pixiv.net/.*?(logout|login).*").Match(url).Success;
        }

        public bool IsParseUrl(string url)
        {
            return new Regex("https?://www.pixiv.net/.+").Match(url).Success;
        }

        private string GetPixivDisplayMode(UrlContainer.UrlContainer uc)
        {
            var hc = new HtmlContainer.HtmlContainer(uc, _userAccount.Cookies);
            Regex re = new Regex(@"\?mode=(?<Mode>[a-z]+)");

            foreach (Match m in re.Matches(hc.Html))
            {
                if (m.Success)
                {
                    if (m.Groups["Mode"].Value == "manga")
                        return "manga";
                    else if (m.Groups["Mode"].Value == "big")
                        return "big";
                }
            }
            return "medium";
        }

        private bool IsPixivImageUrl(string url)
        {
            return new Regex("https?://i.pximg.net/img-(master|original)/.+$").Match(url).Success;
        }

        public List<UrlContainer.UrlContainer> GetImageUrlList(UrlContainer.UrlContainer uc, string[] format)
        {
            var ret = new List<UrlContainer.UrlContainer>();
            Regex re = new Regex("https?://www.pixiv.net/member_illust.php.*?illust_id=(?<Id>[0-9]+)");

            Match m = re.Match(uc.Url);
            if (m.Success)
            {
                string mode = GetPixivDisplayMode(uc);
                // リファラー必須
                uc.Referer = uc.Url;
                uc.Url = String.Format("http://www.pixiv.net/member_illust.php?mode={0}&illust_id={1}", mode, m.Groups["Id"].Value);
                var hc = new HtmlContainer.HtmlContainer(uc, _userAccount.Cookies);
                hc.UpdateAttributeUrlList("img", "src", format);
                hc.UpdateAttributeUrlList("img", "data-src", format);
                foreach (var cand in hc.AttributeUrlList)
                {
                    if (IsPixivImageUrl(cand.RawUrl))
                    {
                        cand.Referer = uc.Url;
                        ret.Add(cand);
                    }
                }
            }
            return ret;
        }
    }
}
