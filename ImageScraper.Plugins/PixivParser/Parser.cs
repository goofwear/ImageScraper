using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace ImageScraper.Plugins.PixivParser
{
    public class Parser : Plugins.PluginInterface
    {
        bool mEnabled;
        Utilities.Logger mLogger;
        Account mUserAccount;
        PluginForm mPluginForm;
        Uri mBaseUri = new Uri("http://www.pixiv.net/");

        delegate void WriteLogDelegate(string modele, string desc);

        public string Name
        {
            get { return "PixivParser"; }
        }

        public bool Enabled
        {
            get { return mEnabled; }
        }

        public bool IsLoggedIn
        {
            get
            {
                var cookie = GetCookieCollection()["device_token"];
                if (cookie != null && DateTime.Now > cookie.Expires)
                    return false;
                else
                    return mUserAccount.Enabled;
            }
        }

        public Utilities.Logger Logger
        {
            set { mLogger = value; }
        }

        public Parser()
        {
            mEnabled = false;
            mLogger = null;
            mUserAccount = new Account();
        }

        public void SaveSettings()
        {
            Settings settings = new Settings();
            settings.Id = mUserAccount.Id;
            settings.Pass = mUserAccount.Pass;
            settings.Enabled = mEnabled;
            settings.IsLoggedIn = mUserAccount.Enabled;
            if (mUserAccount.Enabled)
                settings.Cookies = Utilities.Common.CookiesToString(GetCookieCollection());
            XmlSerializer xs = new XmlSerializer(typeof(Settings));
            using (StreamWriter sw = new StreamWriter("plugins/" + Name + ".xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, settings);
        }

        public void LoadSettings()
        {
            Settings settings = new Settings();
            XmlSerializer xs = new XmlSerializer(typeof(Settings));
            using (StreamReader sr = new StreamReader("plugins/" + Name + ".xml", new UTF8Encoding(false)))
                settings = (Settings)xs.Deserialize(sr);
            mEnabled = settings.Enabled;
            mUserAccount.Id = settings.Id;
            mUserAccount.Pass = settings.Pass;
            mUserAccount.Enabled = settings.IsLoggedIn;
            if (settings.IsLoggedIn)
            {
                var ccol = Utilities.Common.StringToCookies(settings.Cookies);
                mUserAccount.Cookies.Add(ccol);
            }
        }

        public CookieCollection GetCookieCollection()
        {
            return mUserAccount.Cookies.GetCookies(mBaseUri);
        }

        public void ShowPluginForm()
        {
            if (mPluginForm == null || mPluginForm.IsDisposed)
            {
                mPluginForm = new PluginForm(this);
                mPluginForm.Text = Name;
                mPluginForm.MaximizeBox = false;
                mPluginForm.MinimizeBox = false;
                mPluginForm.SetAccount(mUserAccount);
                mPluginForm.SetEnabled();
                mPluginForm.Show();
            }
        }

        public void PreProcess()
        {
            // フォームが開かれているとき実行されアカウント情報が反映される
            if (mPluginForm != null && !mPluginForm.IsDisposed)
            {
                mEnabled = mPluginForm.GetEnabled();
                if (mEnabled)
                {
                    var userAccount = mPluginForm.GetAccount();
                    SetAccount(userAccount.Id, userAccount.Pass);
                }
                mPluginForm.SetFormEnabled(false);
            }
            // 設定を読み込んだあるいはフォームを閉じたときすでにアカウント情報が反映されている
        }

        public void PostProcess()
        {
            if (mPluginForm != null && !mPluginForm.IsDisposed)
                mPluginForm.SetFormEnabled(true);
        }

        private void OnWriteLog(string module, string desc)
        {
            if (mLogger != null)
                mLogger.WriteInvoke(module, desc);
        }

        internal void SetAccount(string id, string pass)
        {
            if (id != mUserAccount.Id || pass != mUserAccount.Pass)
                mUserAccount = new Account(id, pass);
        }

        internal void SetEnabled(bool enabled)
        {
            mEnabled = enabled;
        }

        private string GetInitConfig(string key)
        {
            var hc = new HtmlContainer.HtmlContainer("https://accounts.pixiv.net/login");
            var jsonString = hc.GetAttribute("input", "value", new Dictionary<string, string>() { { "id", "init-config" } });
            string pat = String.Format(@"{0}.:.(?<Key>[a-f0-9]+).", key);
            Match m = new Regex(pat).Match(jsonString);
            if (m.Success)
            {
                mUserAccount.Cookies.Add(hc.Cookies.GetCookies(mBaseUri));
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
                { "pixiv_id", Uri.EscapeDataString(mUserAccount.Id) },
                { "password", Uri.EscapeDataString(mUserAccount.Pass) },
                { "post_key", GetInitConfig("pixivAccount.postKey") },
                { "return_to", Uri.EscapeDataString(mBaseUri.OriginalString) }
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
            req.CookieContainer = mUserAccount.Cookies;
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            using (var rs = req.GetRequestStream())
            {
                rs.Write(buf, 0, buf.Length);
            }
            var res = req.GetResponse();
            var ccol = req.CookieContainer.GetCookies(mBaseUri);
            req.Abort();

            if (ccol["device_token"] != null)
            {
                mUserAccount.Enabled = true;
                mUserAccount.Cookies.Add(ccol);
                OnWriteLog(Name, "ログインに成功しました");
                return true;
            }
            OnWriteLog(Name, "ログインに失敗しました");
            return false;
        }

        internal bool Login(string id, string pass)
        {
            SetAccount(id, pass);
            return Login(true);
        }

        public bool IsIgnore(string url)
        {
            return new Regex("https?://www.pixiv.net/.*?(logout|login).*").Match(url).Success;
        }

        public bool IsParse(string url)
        {
            return new Regex("https?://www.pixiv.net/.+").Match(url).Success;
        }

        private string GetPixivDisplayMode(UrlContainer.UrlContainer uc)
        {
            var hc = new HtmlContainer.HtmlContainer(uc, mUserAccount.Cookies);
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
                var hc = new HtmlContainer.HtmlContainer(uc, mUserAccount.Cookies);
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
