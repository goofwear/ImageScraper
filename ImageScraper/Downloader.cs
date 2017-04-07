using System;
using System.IO;
using System.Linq;
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
        System.Windows.Forms.Control mFormsControl;
        char[] mInvalidCharsP = Path.GetInvalidPathChars();
        char[] mInvalidCharsF = Path.GetInvalidFileNameChars();

        // メソッド用デリゲート
        public delegate void Delegate_LoggerAdd(object sender, string log, int formIndex);
        public delegate void Delegate_LoggerAddRange(object sender, List<string> log, int formIndex);
        public delegate void Delegate_UpdateStatus(object sender, Status sumStatus);
        public delegate void Delegate_AddProgress(object sender, string title, string url, int max);
        public delegate void Delegate_UpdateProgress(object sender, int downloadCount, int imageCount);
        public delegate void Delegate_FinalizeProgress(object sender);
        public delegate void Delegate_UpdateImageInfo(object sender, ImageInfo info);

        // イベントの定義
        public event Delegate_LoggerAdd Event_LoggerAdd;
        public event Delegate_LoggerAddRange Event_LoggerAddRange;
        public event Delegate_UpdateStatus Event_UpdateStatus;
        public event Delegate_AddProgress Event_AddProgress;
        public event Delegate_UpdateProgress Event_UpdateProgress;
        public event Delegate_FinalizeProgress Event_FinalizeProgress;
        public event Delegate_UpdateImageInfo Event_UpdateImageInfo;

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public bool IsSuspend
        {
            get { return _isSuspend; }
        }

        public Downloader(DownloadSettings Settings, System.Windows.Forms.Control sender)
		{
			this.mSettings = Settings;
			_isRunning = false;
            _isSuspend = false;
			mTempStatus = new Status(0, 0, 0, 0);
			mSumStatus = new Status(0, 0, 0, 0);
            mRootUrlList = new List<string>();
            mCachedUrlSet = new HashSet<string>();
            mFormsControl = sender;
		}

        void OnAddLog(string log, int formIndex)
        {
            if (Event_LoggerAdd != null)
                mFormsControl.Invoke(Event_LoggerAdd, this, log, formIndex);
        }

        void OnAddRangeLog(List<string> log, int formIndex)
        {
            if (Event_LoggerAddRange != null)
                mFormsControl.Invoke(Event_LoggerAddRange, this, log, formIndex);
        }

        void OnUpdateStatus()
        {
            if (Event_UpdateStatus != null)
                mFormsControl.Invoke(Event_UpdateStatus, this, mSumStatus);
        }

        void OnAddProgress(HtmlContainer.HtmlContainer hc, int max)
        {
            if (Event_AddProgress != null)
                mFormsControl.Invoke(Event_AddProgress, this, hc.Title, hc.Container.Url, max);
        }

        void OnUpdateProgress(int downloadCount, int imageCount)
        {
            if (Event_UpdateProgress != null)
                mFormsControl.Invoke(Event_UpdateProgress, this, downloadCount, ++imageCount);
        }

        void OnFinalizeProgress()
        {
            if (Event_UpdateProgress != null)
                mFormsControl.Invoke(Event_FinalizeProgress, this);
        }

        void OnUpdateImageInfo(ImageInfo info)
        {
            if (Event_UpdateImageInfo != null)
            {
                if (String.IsNullOrEmpty(Path.GetExtension(info.ImagePath)))
                {
                    string oldName = info.ImagePath;
                    info.ImagePath += Common.GetImageFormatString(info.ImagePath);
                    File.Move(oldName, info.ImagePath);
                }
                mFormsControl.Invoke(Event_UpdateImageInfo, this, info);
            }
        }

		public async Task<bool> StartTask()
		{
            _isRunning = true;
			bool ret = await Task.Run(() => Mainloop());
			return ret;
		}

		public void StopTask()
		{
			_isRunning = false;
		}

        public bool SuspendTask()
        {
            if (_isRunning && !_isSuspend)
            {
                _isSuspend = true;
                return true;
            }
            return false;
        }

        public bool ResumeTask()
        {
            if (_isRunning && _isSuspend)
            {
                _isSuspend = false;
                return true;
            }
            return false;
        }

        string GetDestination(HtmlContainer.HtmlContainer hc)
        {
            string safeTitle = Common.RemoveChars(hc.Title, mInvalidCharsF);
            string safeLocalPath = Common.RemoveChars(hc.Container.LocalPath, mInvalidCharsP);
            string dir = mSettings.dest;

            if (mSettings.destPlusUrl)
                dir += safeLocalPath;
            if (mSettings.destPlusTitle)
                dir += safeTitle + "\\";

            Directory.CreateDirectory(dir);

            return dir;
        }

        string GetImagePath(UrlContainer.UrlContainer uc, string dir)
        {
            string tempName = Common.RemoveChars(uc.FileName, mInvalidCharsF);
            string newName = mSettings.fileNameGenerator.Generate(dir, tempName);
            return dir + newName;
        }

        ImageInfo SetImageInfo(HtmlContainer.HtmlContainer hc, UrlContainer.UrlContainer uc, string dir)
        {
            ImageInfo info = new ImageInfo();
            info.ParentUrl = hc.Container.Url;
            info.ParentTitle = hc.Title;
            info.ImagePath = GetImagePath(uc, dir);
            return info;
        }

        bool TerminateOrSuspend()
        {
            OnUpdateStatus();
            if (mSettings.checkTerminated.Check(mSumStatus) || !_isRunning)
                return true;

            while (_isSuspend)
                System.Threading.Thread.Sleep(500);

            return false;
        }

        bool WaitDownloadResult()
        {
            bool ret = true;

            if (mTask != null)
                ret = mTask.Result;

            return ret;
        }

        bool Download(UrlContainer.UrlContainer uc, ImageInfo info)
        {
            // ダウンロード
            if (!uc.Cache(mSettings.cookies))
                return false;

            // ファイルサイズによるフィルタリング
            var imageSize = (int)(uc.CachedImageSize / 1000);
            if (mSettings.filterImageSize.Filter(imageSize))
                return false;
            // 解像度によるフィルタリング
            else if (mSettings.filterResolution.Filter(uc.CachedImage.Width, uc.CachedImage.Height))
                return false;
            // カラーフォーマットによるフィルタリング
            else if (mSettings.filterColorFormat.Filter(uc.CachedImage))
                return false;

            // ダウンロードした画像を保存
            uc.SaveCachedImage(info.ImagePath);

            // ダウンロード状況更新
            OnAddLog(uc.Url, 2);
            mTempStatus.size += imageSize;
            mSumStatus.size += imageSize;
            mTempStatus.imageCount++;
            mSumStatus.imageCount++;
            mSettings.filterUrlOverlapped.Add(uc.Url, info);
            OnUpdateImageInfo(info);
            return true;
        }

        bool DownloadWebImages(HtmlContainer.HtmlContainer hc)
		{
            mTempStatus.size = 0;
            mTempStatus.imageCount = 0;
            // ダウンロード履歴のフィルタリング
            hc.AttributeUrlList = mSettings.filterUrlOverlapped.Filter(hc.AttributeUrlList);
            int imageNum = hc.AttributeUrlList.Count;
            // ページあたりの画像枚数のフィルタリング
            if (!mSettings.filterImageCount.Filter(imageNum))
            {
                string dest = GetDestination(hc);
                for (int i = 0; i < imageNum; i++)
                {
                    // 終了条件を満たす
                    if (TerminateOrSuspend())
                        return false;
                    else
                    {
                        // ファイルが存在せず, パスが初期化されている
                        ImageInfo info = SetImageInfo(hc, hc.AttributeUrlList[i], dest);
                        if (!File.Exists(info.ImagePath) && info.ImagePath != null)
                            Download(hc.AttributeUrlList[i], info);
                    }

                    if (mTempStatus.imageCount == 1)
                        OnAddProgress(hc, imageNum);
                    else if (mTempStatus.imageCount > 1)
                        OnUpdateProgress(mTempStatus.imageCount, i);
                }
                if (mTempStatus.imageCount > 0)
                {
                    mSumStatus.pageCount++;
                    OnFinalizeProgress();
                }
                try
                {
                    if (Common.IsEmptyDirectory(dest))
                        Directory.Delete(dest);
                }
                catch { }
            }
            return true;
        }

        bool CheckLogout(string url)
        {
            for (int i = 0; i < mSettings.plugins.Length; i++)
            {
                if (mSettings.plugins[i].Enabled && mSettings.plugins[i].IsLogoutUrl(url))
                    return true;
            }
            return false;
        }

        PluginInterface.PluginInterface Contains(UrlContainer.UrlContainer uc)
        {
            for (int i = 0; i < mSettings.plugins.Length; i++)
            {
                if (mSettings.plugins[i].Enabled && mSettings.plugins[i].IsParseUrl(uc.Url))
                {
                    if (!mSettings.plugins[i].IsLoggedIn)
                    {
                        if (mSettings.plugins[i].Login())
                            mSettings.cookies.Add(mSettings.plugins[i].GetCookieCollection());
                        else
                            throw new ApplicationException(mSettings.plugins[i].Name + "\nログインに失敗しました");
                    }
                    return mSettings.plugins[i];
                }
            }
            return null;
        }

        HtmlContainer.HtmlContainer GetHtmlContainer(UrlContainer.UrlContainer uc)
        {
            OnAddLog(uc.Url, 2);
            if (CheckLogout(uc.Url))
                return null;

            // URLに対応するプラグインを検索，見つかればCookie取得
            PluginInterface.PluginInterface plugin = Contains(uc);
            var hc = new HtmlContainer.HtmlContainer(uc, mSettings.cookies);
            // まず，Htmlを取得しないで済むURLのフィルタリング
            if (!mSettings.filterUrl.Filter(uc.Url))
            {
                // 続いて，Htmlを取得する必要があるタイトルのフィルタリング
                if (!mSettings.filterTitle.Filter(hc.Title))
                {
                    if (plugin != null)
                        hc.AttributeUrlList = plugin.GetImageUrlList(uc, mSettings.format);
                    else
                    {
                        if (mSettings.enabledHref)
                            hc.UpdateAttributeUrlList("a", "href", mSettings.format);
                        if (mSettings.enabledIsrc)
                        {
                            hc.UpdateAttributeUrlList("img", "src", mSettings.format);
                            hc.UpdateAttributeUrlList("img", "data-src", mSettings.format);
                        }
                    }
                }
            }
            return hc;
        }

        bool SendLink(List<UrlContainer.UrlContainer> urlList)
        {
            foreach (var url in urlList)
            {
                // 終了条件を満たす
                if (TerminateOrSuspend())
                    return false;

                if (!mCachedUrlSet.Contains(url.RawUrl))
                {
                    var hc = GetHtmlContainer(url);
                    if (hc != null && hc.AttributeUrlList.Count > 0)
                    {
                        if (!WaitDownloadResult())
                            return false;
                        mTask = new Task<bool>(() => DownloadWebImages(hc));
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
                var hc = new HtmlContainer.HtmlContainer(rootUrl, mSettings.cookies);
                hc.UpdateAttributeUrlList("a", "href", new string[] { "php", "html", "htm", "" });
                OnAddRangeLog(hc.AttributeUrlList.Select(x => x.Url).ToList(), 1);
                // ドメインのフィルタリング
                var tmpUrlList = mSettings.filterDomain.Filter(hc.AttributeUrlList);

                if (!SendLink(tmpUrlList))
                    return false;
			}
            return WaitDownloadResult();
		}

        bool Mainloop()
        {
            var hc = GetHtmlContainer(mSettings.urlContainer);
            if (hc == null)
                return false;

            if (DownloadWebImages(hc))
            {
                mCachedUrlSet.Add(mSettings.urlContainer.RawUrl);
                mRootUrlList.Add(mSettings.urlContainer.RawUrl);
                while (true)
                {
                    if (!SendRootLink())
                        break;
                    mSumStatus.depthCount++;
                }
            }
            return true;
        }
	}
}
