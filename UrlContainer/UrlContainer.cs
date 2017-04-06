using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace UrlContainer
{
    public class UrlContainer
    {
        public string Url { get; set; }
        public string ResponseUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string Referer { get; set; }
        public string AttributeName { get; set; }
        public Image CachedImage = null;
        public long CachedImageSize = 0;
        UrlParser Parse;

        public static int RequestSpan = 500;

        public string RawUrl
        {
            get { return Parse.RawUrl; }
        }

        public string LocalPath
        {
            get { return Parse.LocalPath; }
        }

        public string FileName
        {
            get { return Parse.FileName; }
        }

        public string Extension
        {
            get { return Parse.Extension; }
        }

        public string Authority
        {
            get { return Parse.Authority; }
        }

        public UrlContainer(string url, string referer = null)
        {
            this.Url = WebUtility.HtmlDecode(url);
            this.ResponseUrl = "";
            this.DownloadUrl = this.Url;
            this.Referer = referer;
            this.Parse = new UrlParser(this.Url);
        }

        public bool Cache(CookieContainer cc)
        {
            Task task = new Task<bool>(() =>
            {
                Thread.Sleep(RequestSpan);
                return true;
            });

            if (!String.IsNullOrEmpty(this.DownloadUrl))
            {
                // WebRequestを作成
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.DownloadUrl);
                req.Referer = this.Referer;
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
                req.Timeout = 10 * 1000; // 10 sec timeout
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(cc.GetCookies(new Uri(this.DownloadUrl)));

                try
                {
                    task.Start();
                    // サーバーからの応答を受信するためのWebResponseを取得
                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                    // 応答データを受信するためのStreamを取得
                    using (Stream rs = res.GetResponseStream())
                    // ファイルに書き込むためのFileStreamを作成
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int readSize = 0;
                        byte[] buffer = new byte[65536];
                        while ((readSize = rs.Read(buffer, 0, buffer.Length)) > 0)
                            // 読み込んだデータをストリームに書き込む
                            ms.Write(buffer, 0, readSize);
                        CachedImage = new Bitmap(Image.FromStream(ms));
                        CachedImageSize = ms.Length;
                    }
                    task.Wait();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    req.Abort();
                }
            }
            return true;
        }

        public void SaveCachedImage(string path)
        {
            CachedImage.Save(path);
            CachedImage.Dispose();
            CachedImage = null;
            CachedImageSize = 0;
        }

        public bool Download(string path, CookieContainer cc)
        {
            Task task = new Task<bool>(() => 
            {
                Thread.Sleep(RequestSpan);
                return true;
            });

            if (!String.IsNullOrEmpty(this.DownloadUrl))
            {
                // WebRequestを作成
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.DownloadUrl);
                req.Referer = this.Referer;
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
                req.Timeout = 10 * 1000; // 10 sec timeout
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(cc.GetCookies(new Uri(this.DownloadUrl)));

                try
                {
                    task.Start();
                    // サーバーからの応答を受信するためのWebResponseを取得
                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                    // 応答データを受信するためのStreamを取得
                    using (Stream rs = res.GetResponseStream())
                    // ファイルに書き込むためのFileStreamを作成
                    using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        int readSize = 0;
                        byte[] buffer = new byte[65536];
                        while ((readSize = rs.Read(buffer, 0, buffer.Length)) > 0)
                            // 読み込んだデータをストリームに書き込む
                            fs.Write(buffer, 0, readSize);
                    }
                    task.Wait();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    req.Abort();
                }
            }
            return true;
        }

        public string GetResponseUrl(CookieContainer cc)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.Url);
            req.Referer = this.Referer;
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
            req.Timeout = 10 * 1000; // 10 sec timeout
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(cc.GetCookies(new Uri(this.Url)));

            try
            {
                // サーバーからの応答を受信するためのHttpWebResponseを取得
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                this.ResponseUrl = res.ResponseUri.ToString();
            }
            catch
            {
                return "";
            }
            finally
            {
                req.Abort();
            }
            return this.ResponseUrl;
        }
    }
}
