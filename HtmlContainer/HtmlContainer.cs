using System;
using System.IO;
using System.Net;
using System.Web;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HtmlContainer
{
    public class HtmlContainer
    {
        public UrlContainer.UrlContainer UrlContainer;
        public List<UrlContainer.UrlContainer> AttributeUrlList;
        public CookieContainer Cookies;

        public static int RequestSpan = 500;
        public static WebProxy Proxy = null;

        string _html = null;
        string _title = null;

        public string Html
        {
            get
            {
                if (_html == null)
                    _html = Download();

                return _html;
            }
        }

        public string Title
        {
            get
            {
                if (_title == null)
                    _title = GetTitle();

                return _title;
            }
        }

        public HtmlContainer(string url, CookieContainer cc = null)
        {
            this.Cookies = cc;
            this.UrlContainer = new UrlContainer.UrlContainer(url);
            this.AttributeUrlList = new List<UrlContainer.UrlContainer>();
        }

        public HtmlContainer(UrlContainer.UrlContainer uc, CookieContainer cc = null)
        {
            this.Cookies = cc;
            this.UrlContainer = uc;
            this.AttributeUrlList = new List<UrlContainer.UrlContainer>();
        }

        private string Decode(HttpWebResponse res)
        {
            Encoding enc = Encoding.UTF8;

            // まず、HTTPヘッダーのContent-Typeフィールドを見る
            string charset = res.CharacterSet;
            if (!string.IsNullOrWhiteSpace(charset))
            {
                try
                {
                    // Content-TypeフィールドのcharsetパラメーターからEncodingの生成に成功したら、それを返す
                    enc = Encoding.GetEncoding(charset);
                }
                catch { }
            }

            // 次に、HTMLの中でcharset属性を探す
            var ms = new MemoryStream();
            using (Stream sr = res.GetResponseStream())
            {
                int readSize = 0;
                byte[] buffer = new byte[65536];
                while ((readSize = sr.Read(buffer, 0, buffer.Length)) > 0)
                    // 読み込んだデータをストリームに書き込む
                    ms.Write(buffer, 0, readSize);
            }
            byte[] rawHtml = ms.ToArray();
            ms.Close();

            // 取りあえずUTF-8だとして読んでみる
            string html = Encoding.UTF8.GetString(rawHtml);

            // charset属性を探す
            // HTML4の <meta http-equiv="Content-Type" content="text/html; charset=｛エンコーディング名｝"> 
            // HTML5の <meta charset="｛エンコーディング名｝">
            // HTML4／5の <｛任意の要素名｝ charset="｛エンコーディング名｝">
            var charsetEx = new Regex(@"<[^>]*\bcharset\s*=\s*[""']?(?<Charset>\w+)\b",
                                      RegexOptions.CultureInvariant
                                      | RegexOptions.IgnoreCase
                                      | RegexOptions.Singleline);
            Match charsetMatch = charsetEx.Match(html);
            if (charsetMatch.Success)
            {
                try
                {
                    // 発見した最初のcharset属性からEncodingの生成に成功したら、それを返す
                    enc = Encoding.GetEncoding(charsetMatch.Groups["Charset"].Value);
                }
                catch { }
            }
            return enc.GetString(rawHtml);
        }

        private string Download()
        {
            Task task = new Task<bool>(() =>
            {
                Thread.Sleep(RequestSpan);
                return true;
            });

            string html = null;
            // HttpWebRequestを作成
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(UrlContainer.Url);
            req.Referer = UrlContainer.Referer;
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
            req.Timeout = 10 * 1000; // 10 sec timeout
            req.CookieContainer = new CookieContainer();
            if (Proxy != null)
                req.Proxy = Proxy;

            if (Cookies != null)
                req.CookieContainer.Add(Cookies.GetCookies(new Uri(UrlContainer.Url)));
            else
                Cookies = new CookieContainer();

            try
            {
                task.Start();
                // サーバーからの応答を受信するためのHttpWebResponseを取得
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                html = Decode(res);
                Cookies.Add(req.CookieContainer.GetCookies(new Uri(UrlContainer.Url)));
                task.Wait();
            }
            catch
            {
                return "";
            }
            finally
            {
                req.Abort();
            }
            return html;
        }

        private string GetTitle()
        {
            if (Html == null)
                return null;

            string title = "";
            Regex re = new Regex("<title>(?<Title>.*?)</title>", RegexOptions.IgnoreCase);
            Match m = re.Match(Html);

            if (m.Success == true)
                title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
            
            return title;
        }

        public void UpdateAttributeUrlList(string tag, string attr, string[] format)
        {
            if (Html == null)
                return;

            var hp = new HtmlParser();
            hp.Source = Html;
            Regex re = new Regex(@"^(https?|ftp)://.+", RegexOptions.IgnoreCase);

            while (!hp.Eof())
            {
                if (hp.Parse() == 0)
                {
                    var attrList = hp.GetTag();
                    if (attrList.Name == tag && attrList[attr] != null)
                    {
                        string attrValue = attrList[attr].Value;
                        if (!re.Match(attrValue).Success)
                        {
                            Uri baseUrl = new Uri(UrlContainer.Url);
                            Uri abs = new Uri(baseUrl, attrValue);
                            attrValue = abs.AbsoluteUri;
                        }
                        if (re.Match(attrValue).Success)
                        {
                            var uc = new UrlContainer.UrlContainer(attrValue, UrlContainer.RawUrl);
                            uc.AttributeName = attr;
                            if (format == null || format.Contains(uc.Extension))
                                AttributeUrlList.Add(uc);
                        }
                    }
                }
            }
            AttributeUrlList = AttributeUrlList.Distinct().ToList();
        }
    }
}
