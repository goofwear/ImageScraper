﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using Utilities;

namespace SeigaParsePlugin
{
    public class Parser : PluginInterface
    {
        bool mEnabled;
        Account mUserAccount;
        PluginForm mPluginForm;
        LoggerDelegate mLoggerDelegate;
        Uri mBaseUri = new Uri("http://seiga.nicovideo.jp/");

        public string Name
        {
            get { return "SeigaParsePlugin"; }
        }

        public bool Enabled
        {
            get { return mEnabled; }
        }

        public bool IsLoggedIn
        {
            get
            {
                var cookie = GetCookieCollection()["user_session"];
                if (cookie != null && DateTime.Now > cookie.Expires)
                    return false;
                else
                    return mUserAccount.Enabled;
            }
        }

        public Parser()
        {
            mEnabled = false;
            mUserAccount = new Account();
            mLoggerDelegate = null;
        }

        public void SetLoggerDelegate(LoggerDelegate loggerDelegate)
        {
            mLoggerDelegate = loggerDelegate;
        }

        public void SaveSettings()
        {
            Settings settings = new Settings();
            settings.Id = mUserAccount.Id;
            settings.Pass = mUserAccount.Pass;
            settings.Enabled = mEnabled;
            settings.IsLoggedIn = mUserAccount.Enabled;
            if (mUserAccount.Enabled)
                settings.Cookies = Common.CookiesToString(GetCookieCollection());
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
                var ccol = Common.StringToCookies(settings.Cookies);
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

        internal void SetAccount(string id, string pass)
        {
            if (id != mUserAccount.Id || pass != mUserAccount.Pass)
                mUserAccount = new Account(id, pass);
        }

        internal void SetEnabled(bool enabled)
        {
            mEnabled = enabled;
        }

        public bool Login(bool force = false)
        {
            if (IsLoggedIn && !force)
                return true;

            const string path = "https://secure.nicovideo.jp/secure/login?site=niconico";
            var req = (HttpWebRequest)WebRequest.CreateHttp(new Uri(path));
            var param = String.Format("nextmUrl={0}&mail={1}&password={2}", "", 
                Uri.EscapeDataString(mUserAccount.Id), Uri.EscapeDataString(mUserAccount.Pass));
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
            var ccol = req.CookieContainer.GetCookies(mBaseUri);
            req.Abort();

            if (ccol["user_session"] != null)
            {
                ccol.Add(new Cookie("accept_fetish_warning", "1", "/", "seiga.nicovideo.jp"));
                mUserAccount.Enabled = true;
                mUserAccount.Cookies.Add(ccol);
                mLoggerDelegate.Write(Name, "ログインに成功しました");
                return true;
            }
            mLoggerDelegate.Write(Name, "ログインに失敗しました");
            return false;
        }

        internal bool Login(string id, string pass)
        {
            SetAccount(id, pass);
            return Login(true);
        }

        public bool IsIgnore(string url)
        {
            return new Regex("https?://.+?.nicovideo.jp/.*?(logout|login).*").Match(url).Success;
        }

        public bool IsParse(string url)
        {
            return new Regex("https?://seiga.nicovideo.jp/.+").Match(url).Success;
        }

        private string GetSeigaDisplayMode(UrlContainer.UrlContainer uc)
        {
            Regex re = new Regex("https?://seiga.nicovideo.jp/(?<Mode>seiga|watch)");
            Match m = re.Match(uc.Url);
            if (m.Success)
                return m.Groups["Mode"].Value;
            return null;
        }

        public List<UrlContainer.UrlContainer> GetImageUrlList(UrlContainer.UrlContainer uc, string[] format)
        {
            var ret = new List<UrlContainer.UrlContainer>();
            string mode = GetSeigaDisplayMode(uc);

            if (mode == "seiga")
            {
                Regex re = new Regex("https?://seiga.nicovideo.jp/seiga/im(?<Id>[0-9]+)$");
                Match m = re.Match(uc.Url);
                if (m.Success)
                {
                    uc.Url = "http://seiga.nicovideo.jp/image/source?id=" + m.Groups["Id"].Value;
                    string resUrl = uc.GetResponseUrl(mUserAccount.Cookies);
                    uc.DownloadUrl = resUrl.Replace("/o/", "/priv/");
                    if (!String.IsNullOrEmpty(uc.DownloadUrl))
                        ret.Add(uc);
                }
            }
            else if (mode == "watch")
            {
                var hc = new HtmlContainer.HtmlContainer(uc, mUserAccount.Cookies);
                hc.UpdateAttributeUrlList("img", "data-original", null);
                ret = hc.AttributeUrlList;
            }
            return ret;
        }
    }
}
