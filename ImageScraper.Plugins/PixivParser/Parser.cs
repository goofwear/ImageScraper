using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Linq;

namespace ImageScraper.Plugins.PixivParser
{
    public class Parser : Plugins.IPlugin
    {
        Utilities.Logger mLogger;
        Account mUserAccount;
        PluginForm mPluginForm;
        Uri mBaseUri = new Uri("https://www.pixiv.net/");

        delegate void WriteLogDelegate(string modele, string desc);

        public string Name
        {
            get { return "PixivParser"; }
        }

        public bool Enabled { get; private set; }

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

        public bool IsExclusive
        {
            get { return true; }
        }

        public Utilities.Logger Logger
        {
            set { mLogger = value; }
        }

        public Parser()
        {
            Enabled = false;
            mLogger = null;
            mUserAccount = new Account();
        }

        public void SaveSettings()
        {
            Settings settings = new Settings();
            settings.Id = mUserAccount.Id;
            settings.Pass = mUserAccount.Pass;
            settings.Enabled = Enabled;
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
            Enabled = settings.Enabled;
            mUserAccount.Id = settings.Id;
            mUserAccount.Pass = settings.Pass;
            mUserAccount.Enabled = settings.IsLoggedIn;
            if (settings.IsLoggedIn)
            {
                var ccol = Utilities.Common.StringToCookies(settings.Cookies);
                mUserAccount.CookieContainer.Add(ccol);
            }
        }

        public CookieCollection GetCookieCollection()
        {
            return mUserAccount.CookieContainer.GetCookies(mBaseUri);
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
                Enabled = mPluginForm.GetEnabled();
                if (Enabled)
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
                mLogger.Write(module, desc);
        }

        internal void SetAccount(string id, string pass)
        {
            if (id != mUserAccount.Id || pass != mUserAccount.Pass)
                mUserAccount = new Account(id, pass);
        }

        internal void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        private string GetPostKey()
        {
            var hc = new HtmlContainer.HtmlContainer("https://accounts.pixiv.net/login");
            Match m = new Regex("\"post_key\" value=\"(?<Key>[a-z0-9]+)\"").Match(hc.Html);
            if (m.Success)
            {
                mUserAccount.CookieContainer.Add(hc.CookieContainer.GetCookies(mBaseUri));
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
                { "post_key", GetPostKey() },
                { "source", "pc" },
            };
            foreach (var pair in content)
                param += String.Format("{0}={1}&", pair.Key, pair.Value);

            var buf = Encoding.UTF8.GetBytes(param.TrimEnd('&'));
            var uri = new Uri("https://accounts.pixiv.net/api/login?lang=ja");
            var req = (HttpWebRequest)WebRequest.CreateHttp(uri);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            req.CookieContainer = mUserAccount.CookieContainer;
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
            req.Referer = "https://accounts.pixiv.net/login";

            using (var rs = req.GetRequestStream())
                rs.Write(buf, 0, buf.Length);
            var res = req.GetResponse();
            var ccol = req.CookieContainer.GetCookies(mBaseUri);
            req.Abort();

            if (ccol["device_token"] != null && ccol["PHPSESSID"] != null)
            {
                mUserAccount.Enabled = true;
                mUserAccount.CookieContainer.Add(ccol);
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

        private List<UrlContainer.UrlContainer> PixivImageUrls(HtmlContainer.HtmlContainer hc)
        {
            var html = hc.Html.Replace("\\/", "/");
            var urls = new List<UrlContainer.UrlContainer>();
            var re = new Regex(@"https?://i.pximg.net/img-original/[a-z0-9\./_]+");
            foreach (Match m in re.Matches(html))
            {
                if (m.Success)
                {
                    var uc = new UrlContainer.UrlContainer(m.Value, hc.UrlContainer.Url);
                    urls.Add(uc);
                }
            }
            return urls;
        }

        private List<UrlContainer.UrlContainer> PixivMangaUrls(HtmlContainer.HtmlContainer hc, string[] format)
        {
            var urls = new List<UrlContainer.UrlContainer>();
            var re = new Regex(@"mode=manga_big");
            var backup = HtmlContainer.HtmlContainer.RequestSpan;

            HtmlContainer.HtmlContainer.RequestSpan = 0;
            hc.UpdateAttributeUrlList("a", "href", null);
            foreach (var uc in hc.AttributeUrlList)
            {
                Match m = re.Match(uc.Url);
                if (m.Success)
                {
                    var tmp = new HtmlContainer.HtmlContainer(uc.Url, hc.CookieContainer);
                    tmp.UpdateAttributeUrlList("img", "src", format);
                    urls.AddRange(tmp.AttributeUrlList);
                }
            }
            HtmlContainer.HtmlContainer.RequestSpan = backup;

            return urls;
        }

        public List<UrlContainer.UrlContainer> GetLinkList(HtmlContainer.HtmlContainer hc)
        {
            List<UrlContainer.UrlContainer> ret = new List<UrlContainer.UrlContainer>();
            var re = new Regex(@"""illustId"":""(?<Id>[0-9]+)""");
            foreach (Match m in re.Matches(hc.Html))
            {
                if (m.Success)
                {
                    var uc = new UrlContainer.UrlContainer(
                        "https://www.pixiv.net/member_illust.php?mode=medium&illust_id=" + m.Groups["Id"].Value,
                        hc.UrlContainer.Url);
                    ret.Add(uc);
                }
            }
            return ret;
        }

        public List<UrlContainer.UrlContainer> GetImageUrlList(UrlContainer.UrlContainer uc, string[] format)
        {
            var ret = new List<UrlContainer.UrlContainer>();
            Regex re = new Regex("https?://www.pixiv.net/member_illust.php.*?illust_id=(?<Id>[0-9]+)");

            Match m = re.Match(uc.Url);
            if (m.Success)
            {
                var hc = new HtmlContainer.HtmlContainer(uc, mUserAccount.CookieContainer);
                foreach (var url in PixivImageUrls(hc))
                    ret.Add(url);
                hc = new HtmlContainer.HtmlContainer(
                    new UrlContainer.UrlContainer("https://www.pixiv.net/member_illust.php?mode=manga&illust_id=" + m.Groups["Id"].Value), 
                    mUserAccount.CookieContainer);
                foreach (var url in PixivMangaUrls(hc, format))
                    ret.Add(url);
            }
            return ret;
        }
    }
}
