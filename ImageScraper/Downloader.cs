using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageScraper
{
	public class Downloader
	{
		bool _isRunning;
        bool _isSuspend;
        DownloadSettings mSettings;
        List<string> mRootUrlList;
        HashSet<string> mCachedUrlSet;
        Task<bool> mTask = null;
        Status mTempStatus;
        Status mSumStatus;
        CookieContainer mCookies;
        MainForm mParentForm;
        Plugins.PluginInterface[] mPlugins;
        char[] mInvalidCharsP = Path.GetInvalidPathChars();
        char[] mInvalidCharsF = Path.GetInvalidFileNameChars();

        delegate void WriteLogDelegate(string modele, string desc);

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public bool IsSuspend
        {
            get { return _isSuspend; }
        }

        public Downloader(DownloadSettings settings, Plugins.PluginInterface[] plugins, MainForm parentForm)
		{
			mSettings = settings;
            mPlugins = plugins;
            mCookies = new CookieContainer();
			_isRunning = false;
            _isSuspend = false;
			mTempStatus = new Status(0, 0, 0, 0);
			mSumStatus = new Status(0, 0, 0, 0);
            mRootUrlList = new List<string>();
            mCachedUrlSet = new HashSet<string>();
            mParentForm = parentForm;
		}

        void OnUpdateStatus()
        {
            mParentForm.Invoke(new Action(() => mParentForm.UpdateStatus(mSumStatus)));
        }

        void OnInitProgress(HtmlContainer.HtmlContainer hc, int max)
        {
            mParentForm.Invoke(new Action(() => mParentForm.InitProgress(hc.Title, hc.UrlContainer.Url, max)));
        }

        void OnUpdateProgress(int downloadCount, int imageCount)
        {
            mParentForm.Invoke(new Action(() => mParentForm.UpdateProgress(downloadCount, imageCount + 1)));
        }

        void OnFinalizeProgress()
        {
            mParentForm.Invoke(new Action(() => mParentForm.FinalizeProgress()));
        }

        void OnUpdateImageInfo(ImageInfo info)
        {
            if (String.IsNullOrEmpty(Path.GetExtension(info.ImagePath)))
            {
                string oldName = info.ImagePath;
                info.ImagePath += Utilities.Common.GetImageFormatString(info.ImagePath);
                File.Move(oldName, info.ImagePath);
            }
            mParentForm.Invoke(new Action(() => mParentForm.UpdateImageInfo(info)));
        }

		public async Task<bool> Start()
		{
            _isRunning = true;
			bool ret = await Task.Run(() => Mainloop());
			return ret;
		}

		public void Stop()
		{
			_isRunning = false;
		}

        public bool Suspend()
        {
            if (_isRunning && !_isSuspend)
            {
                _isSuspend = true;
                return true;
            }
            return false;
        }

        public bool Resume()
        {
            if (_isRunning && _isSuspend)
            {
                _isSuspend = false;
                return true;
            }
            return false;
        }

        string InitDirectoryName(HtmlContainer.HtmlContainer hc)
        {
            string safeTitle = Utilities.Common.RemoveChars(hc.Title, mInvalidCharsF, "_");
            string safeLocalPath = Utilities.Common.RemoveChars(hc.UrlContainer.LocalPath, mInvalidCharsP, "_");
            string dir = mSettings.RootDirectory;

            if (mSettings.AppendsUrl)
                dir += safeLocalPath;
            if (mSettings.AppendsTitle)
                dir += safeTitle + "\\";

            Directory.CreateDirectory(dir);

            return dir;
        }

        ImageInfo InitImageInfo(HtmlContainer.HtmlContainer hc, UrlContainer.UrlContainer uc, string dir)
        {
            string tmpName = Utilities.Common.RemoveChars(uc.FileName, mInvalidCharsF, "_");
            string fileName = mSettings.FileNameGenerator.Generate(dir, tmpName);

            ImageInfo info = new ImageInfo();
            info.ImageUrl = uc.Url;
            info.ParentUrl = hc.UrlContainer.Url;
            info.ParentTitle = hc.Title;
            info.ImagePath = dir + fileName;

            return info;
        }

        bool HasCompleted()
        {
            OnUpdateStatus();
            if (mSettings.StatusMonitor.HasCompleted(mSumStatus) || !_isRunning)
                return true;

            while (_isSuspend)
                System.Threading.Thread.Sleep(500);

            return false;
        }

        bool HasDaemonCompleted()
        {
            bool ret = true;

            if (mTask != null)
                ret = mTask.Result;

            return ret;
        }

        bool Download(UrlContainer.UrlContainer uc, ImageInfo info)
        {
            // ダウンロード
            if (!uc.Cache(mCookies))
                return false;

            // ファイルサイズによるフィルタリング
            var imageSize = (int)(uc.CacheSize / 1000);
            if (mSettings.ImageSizeFilter.Filter(imageSize))
            {
                mSettings.Logger.Write("Downloader", "ファイルサイズフィルタが適用されました > " + uc.Url);
                return false;
            }

            using (var cachedImage = new Bitmap(Image.FromStream(uc.CacheStream)))
            {
                // 解像度によるフィルタリング
                if (mSettings.ResolutionFilter.Filter(cachedImage))
                {
                    mSettings.Logger.Write("Downloader", "解像度フィルタが適用されました > " + uc.Url);
                    return false;
                }
                // カラーフォーマットによるフィルタリング
                if (mSettings.ColorFilter.Filter(cachedImage))
                {
                    mSettings.Logger.Write("Downloader", "カラーフィルタが適用されました > " + uc.Url);
                    return false;
                }
            }

            // ダウンロードした画像を保存
            uc.SaveCache(info.ImagePath);

            // ダウンロード状況更新
            mSettings.Logger.Write("Downloader", uc.Url + " を取得しました");
            mTempStatus.Size += imageSize;
            mSumStatus.Size += imageSize;
            mTempStatus.Images++;
            mSumStatus.Images++;
            mSettings.OverlappedUrlFilter.Add(info);
            OnUpdateImageInfo(info);
            return true;
        }

        bool DownloadImages(HtmlContainer.HtmlContainer hc)
		{
            mTempStatus.Size = 0;
            mTempStatus.Images = 0;
            int images = hc.AttributeUrlList.Count;
            // ページあたりの画像枚数のフィルタリング
            if (mSettings.ImagesPerPageFilter.Filter(images))
            {
                string mes = String.Format("{0} 枚 ({1})", images, hc.UrlContainer.Url);
                mSettings.Logger.Write("Downloader", "画像枚数フィルタが適用されました > " + mes);
            }
            else
            {
                // 重複ダウンロードのフィルタリング
                hc.AttributeUrlList = mSettings.OverlappedUrlFilter.Filter(hc.AttributeUrlList);
                // 画像枚数更新
                images = hc.AttributeUrlList.Count;
                string dir = InitDirectoryName(hc);
                for (int i = 0; i < images; i++)
                {
                    // 終了条件を満たす
                    if (HasCompleted())
                        return false;
                    else if (!mSettings.ImageUrlFilter.Filter(hc.AttributeUrlList[i].Url))
                    {
                        // ファイルが存在せず, パスが初期化されている
                        ImageInfo info = InitImageInfo(hc, hc.AttributeUrlList[i], dir);
                        if (!File.Exists(info.ImagePath) && info.ImagePath != null)
                            Download(hc.AttributeUrlList[i], info);
                    }
                    else
                        mSettings.Logger.Write("Downloader", "画像 URL フィルタが適用されました > " + hc.AttributeUrlList[i].Url);

                    if (mTempStatus.Images == 1)
                        OnInitProgress(hc, images);
                    else if (mTempStatus.Images > 1)
                        OnUpdateProgress(mTempStatus.Images, i);
                }
                if (mTempStatus.Images > 0)
                {
                    mSumStatus.Pages++;
                    OnFinalizeProgress();
                }
                Utilities.Common.DeleteEmptyDirectory(dir);
            }
            return true;
        }

        bool IsIgnore(string url)
        {
            for (int i = 0; i < mPlugins.Length; i++)
            {
                if (mPlugins[i].Enabled && mPlugins[i].IsIgnore(url))
                    return true;
            }
            return false;
        }

        Plugins.PluginInterface FindPlugin(UrlContainer.UrlContainer uc)
        {
            foreach (var plugin in mPlugins)
            {
                if (plugin.Enabled && plugin.IsParse(uc.Url))
                {
                    if (plugin.Login())
                        mCookies.Add(plugin.GetCookieCollection());
                    else
                        throw new ApplicationException(plugin.Name + "\nログインに失敗しました");
                    return plugin;
                }
            }
            return null;
        }

        HtmlContainer.HtmlContainer GetHtmlContainer(UrlContainer.UrlContainer uc)
        {
            if (IsIgnore(uc.Url))
                return null;

            // URLに対応するプラグインを検索，見つかればCookie取得
            Plugins.PluginInterface plugin = FindPlugin(uc);
            var hc = new HtmlContainer.HtmlContainer(uc, mCookies);

            // Htmlを取得する必要がないURLのフィルタリング
            if (mSettings.UrlFilter.Filter(uc.Url))
            {
                mSettings.Logger.Write("Downloader", "URL フィルタが適用されました > " + uc.Url);
                return hc;
            }
            // Htmlを取得する必要があるタイトルのフィルタリング
            if (mSettings.TitleFilter.Filter(hc.Title))
            {
                mSettings.Logger.Write("Downloader", "タイトルフィルタが適用されました > " + hc.Title);
                return hc;
            }

            mSettings.Logger.Write("Downloader", uc.Url + " を取得しました");   
            if (plugin != null && plugin.IsExclusive)
                hc.AttributeUrlList = plugin.GetImageUrlList(uc, mSettings.Formats);
            else
            {
                if (mSettings.ParseHrefAttr)
                    hc.UpdateAttributeUrlList("a", "href", mSettings.Formats);
                if (mSettings.ParseImgTag)
                {
                    hc.UpdateAttributeUrlList("img", "src", mSettings.Formats);
                    hc.UpdateAttributeUrlList("img", "data-src", mSettings.Formats);
                }
            }
            return hc;
        }

        bool SendLink(List<UrlContainer.UrlContainer> urlList)
        {
            foreach (var url in urlList)
            {
                // 終了条件を満たす
                if (HasCompleted())
                    return false;

                if (!mCachedUrlSet.Contains(url.RawUrl))
                {
                    var hc = GetHtmlContainer(url);
                    if (hc != null && hc.AttributeUrlList.Count > 0)
                    {
                        if (!HasDaemonCompleted())
                            return false;
                        mTask = new Task<bool>(() => DownloadImages(hc));
                        mTask.Start();
                    }
                    // 順番を保持するのでList
                    mRootUrlList.Add(url.RawUrl);
                    // 検索かけるのでHashSet
                    mCachedUrlSet.Add(url.RawUrl);
                }
            }
            return true;
        }

		bool SendRootLink()
		{
            var rootUrlList = new List<string>(mRootUrlList);
            mRootUrlList.Clear();

            if (rootUrlList.Count == 0)
                return false;

            foreach (var rootUrl in rootUrlList)
			{
                if (mSettings.RootUrlFilter.Filter(rootUrl))
                    mSettings.Logger.Write("Downloader", "ルート URL フィルタが適用されました > " + rootUrl);
                else
                {
                    var hc = new HtmlContainer.HtmlContainer(rootUrl, mCookies);
                    hc.UpdateAttributeUrlList("a", "href", new string[] { "php", "phtml", "html", "htm", "" });
                    // ドメインのフィルタリング
                    var tmpUrlList = mSettings.DomainFilter.Filter(hc.AttributeUrlList);

                    if (!SendLink(tmpUrlList))
                        return false;
                }
			}
            return HasDaemonCompleted();
		}

        bool Mainloop()
        {
            var hc = GetHtmlContainer(mSettings.UrlContainer);
            if (hc == null)
                return false;

            if (DownloadImages(hc))
            {
                mCachedUrlSet.Add(mSettings.UrlContainer.RawUrl);
                mRootUrlList.Add(mSettings.UrlContainer.RawUrl);
                while (true)
                {
                    if (!SendRootLink())
                        break;
                    mSumStatus.Depth++;
                }
            }
            return true;
        }
	}
}
