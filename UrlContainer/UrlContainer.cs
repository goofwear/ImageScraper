using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace UrlContainer
{
    public class UrlContainer : IEquatable<UrlContainer>
    {
        public string Url { get; set; }
        public string ResponseUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string Referer { get; set; }
        public string AttributeName { get; set; }
        public MemoryStream CacheStream = null;
        public long CacheSize = 0;
        UrlParser Parser;

        public static int RequestSpan = 500;
        public static WebProxy Proxy = null;

        public string RawUrl
        {
            get { return Parser.RawUrl; }
        }

        public string LocalPath
        {
            get { return Parser.LocalPath; }
        }

        public string FileName
        {
            get { return Parser.FileName; }
        }

        public string Extension
        {
            get { return Parser.Extension; }
        }

        public string Authority
        {
            get { return Parser.Authority; }
        }

        public UrlContainer(string url, string referer = null)
        {
            this.Url = WebUtility.HtmlDecode(url);
            this.ResponseUrl = "";
            this.DownloadUrl = this.Url;
            this.Referer = referer;
            this.Parser = new UrlParser(this.Url);
        }

        public override int GetHashCode()
        {
            return this.RawUrl.GetHashCode();
        }

        bool IEquatable<UrlContainer>.Equals(UrlContainer urlContainer)
        {
            if (urlContainer == null)
                return false;
            return (this.RawUrl == urlContainer.RawUrl);
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
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                req.Timeout = 10 * 1000; // 10 sec timeout
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(cc.GetCookies(new Uri(this.DownloadUrl)));
                if (Proxy != null)
                    req.Proxy = Proxy;

                try
                {
                    task.Start();
                    // サーバーからの応答を受信するためのWebResponseを取得
                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                    // 応答データを受信するためのStreamを取得
                    using (Stream rs = res.GetResponseStream())
                    { 
                        int readSize = 0;
                        byte[] buffer = new byte[65536];
                        if (CacheStream == null)
                            CacheStream = new MemoryStream();
                        while ((readSize = rs.Read(buffer, 0, buffer.Length)) > 0)
                            // 読み込んだデータをストリームに書き込む
                            CacheStream.Write(buffer, 0, readSize);
                        CacheSize = CacheStream.Length;
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

        public void SaveCache(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                byte[] buffer = new byte[CacheStream.Length];
                CacheStream.Seek(0, SeekOrigin.Begin);
                CacheStream.Read(buffer, 0, buffer.Length);
                fs.Write(buffer, 0, buffer.Length);
                CacheStream.Close();
                CacheStream = null;
                CacheSize = 0;
            }
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
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                req.Timeout = 10 * 1000; // 10 sec timeout
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(cc.GetCookies(new Uri(this.DownloadUrl)));
                if (Proxy != null)
                    req.Proxy = Proxy;

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
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
            req.Timeout = 10 * 1000; // 10 sec timeout
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(cc.GetCookies(new Uri(this.Url)));
            if (Proxy != null)
                req.Proxy = Proxy;

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
