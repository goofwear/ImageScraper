using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Net;
using System.Net.Http;

namespace ImageScraper
{
    partial class MainForm
    {
        private string[] mAvailableFormats = new string[] { "jpg", "jpeg", "png", "bmp", "gif" };

        private void InitializeSettings(DownloadSettings dc)
        {
            // ダウンロード設定
            dc.Logger = mLogger;
            dc.UrlContainer = new UrlContainer.UrlContainer(comboBox1.Text);
            dc.Formats = PickImageFormats();
            dc.ParseHrefAttr = checkBox7.Checked;
            dc.ParseImgTag = checkBox8.Checked;
            dc.DomainFilter = new DomainFilter(checkBox6.Checked, dc.UrlContainer);
            dc.ColorFilter = new ColorFilter(checkBox5.Checked, checkBox20.Checked);
            dc.ImageSizeFilter = new ValueRangeFilter(
                checkBox14.Checked, checkBox17.Checked, (int)numericUpDown1.Value, (int)numericUpDown10.Value);
            dc.ImagesPerPageFilter = new ValueRangeFilter(
                checkBox15.Checked, checkBox18.Checked, (int)numericUpDown2.Value, (int)numericUpDown11.Value);
            dc.TitleFilter = new KeywordFilter(
                checkBox11.Checked, checkBox21.Checked, checkBox22.Checked, comboBox2.Text, comboBox5.Text);
            dc.UrlFilter = new KeywordFilter(
                checkBox12.Checked, checkBox24.Checked, checkBox23.Checked, comboBox3.Text, comboBox4.Text);
            dc.RootUrlFilter = new KeywordFilter(
                checkBox31.Checked, checkBox30.Checked, checkBox29.Checked, comboBox9.Text, comboBox8.Text);
            dc.ImageUrlFilter = new KeywordFilter(
                checkBox28.Checked, checkBox27.Checked, checkBox26.Checked, comboBox7.Text, comboBox6.Text);
            dc.ResolutionFilter = new ResolutionFilter(
                checkBox16.Checked, checkBox19.Checked, (int)numericUpDown5.Value, (int)numericUpDown6.Value, (int)numericUpDown12.Value, (int)numericUpDown13.Value);

            // 保存設定
            var sng = new Utilities.SerialNameGenerator(textBox2.Text, (int)numericUpDown9.Value, mAvailableFormats);
            dc.RootDirectory = textBox5.Text.TrimEnd('\\') + "\\";
            dc.AppendsUrl = checkBox9.Checked;
            dc.AppendsTitle = checkBox10.Checked;
            dc.OverlappedUrlFilter = new OverlappedUrlFilter(mUrlCache, checkBox13.Checked);
            dc.FileNameGenerator = new FileNameGenerator(radioButton2.Checked, sng);

            // 終了条件設定
            var limitStatus = new Status(
                (int)numericUpDown3.Value, (int)numericUpDown8.Value, (int)numericUpDown4.Value, (double)numericUpDown7.Value * 1000);
            dc.StatusMonitor = new StatusMonitor(
                new bool[] { radioButton12.Checked, radioButton10.Checked, radioButton5.Checked, radioButton6.Checked, radioButton7.Checked },
                limitStatus, (int)numericUpDown14.Value, this.CountImages(dc.RootDirectory)
            );

            // 接続設定
            UrlContainer.UrlContainer.RequestSpan = (int)numericUpDown15.Value;
            HtmlContainer.HtmlContainer.RequestSpan = (int)numericUpDown15.Value;
            if (checkBox25.Checked)
            {
                var proxy = new WebProxy(textBox1.Text, int.Parse(textBox3.Text));
                UrlContainer.UrlContainer.Proxy = proxy;
                HtmlContainer.HtmlContainer.Proxy = proxy;
            }
            else
            {
                UrlContainer.UrlContainer.Proxy = null;
                HtmlContainer.HtmlContainer.Proxy = null;
            }
        }

        private void UpdateComboBox(ComboBox cb)
        {
            string str = cb.Text;
            if (!String.IsNullOrEmpty(str))
            {
                // 入力値の重複チェック（アイテム内に無いときは-1が返る）
                if (cb.Items.IndexOf(str) != -1)
                    cb.Items.Remove(str);
                // アイテム一覧の一番上に登録
                cb.Items.Insert(0, str);
                cb.Text = str;
            }
        }

        private void InitializeForm()
        {
            mInfoViewItems.Clear();
            listViewEx1.ClearEmbeddedControl();
            listViewEx1.Items.Clear();
            UpdateComboBox(comboBox1);
            UpdateComboBox(comboBox2);
            UpdateComboBox(comboBox3);
            UpdateComboBox(comboBox4);
            UpdateComboBox(comboBox5);
            UpdateComboBox(comboBox6);
            UpdateComboBox(comboBox7);
            UpdateComboBox(comboBox8);
            UpdateComboBox(comboBox9);
            ReverseControls();
            toolStripStatusLabel1.Text = "ダウンロード中...";
        }

        private void FinalizeForm()
        {
            ReverseControls();
            toolStripStatusLabel1.Text = "完了";
        }

        async void RunDownloader()
        {
            // 入力値のチェック
            SwitchControls(false);
            bool result = await this.IsValidInputs();
            SwitchControls(true);

            if (!result)
                return;

            // フォームを無効化
            InitializeForm();

            try
            {
                // 設定値の反映
                this.InitializeSettings(mDownloadSettings);
                Directory.CreateDirectory(mDownloadSettings.RootDirectory);
                for (int i = 0; i < mPlugins.Length; i++)
                    mPlugins[i].PreProcess();
                mDownloader = new Downloader(mDownloadSettings, mPlugins, this);

                // タスクの実行
                await mDownloader.Start();

                // 後処理
                MessageBox.Show("ダウンロードが完了しました", "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                for (int i = 0; i < mPlugins.Length; i++)
                    mPlugins[i].PostProcess();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // フォームを有効化
                FinalizeForm();
                mDownloader = null;
            }
        }

        private int CountImages(string dir)
        {
            int num = 0;
            if (Directory.Exists(dir))
            {
                for (int i = 0; i < mAvailableFormats.Length; i++)
                    num += Directory.GetFiles(dir, "*." + mAvailableFormats[i], SearchOption.AllDirectories).Length;
            }
            return num;
        }

        private async Task<bool> IsValidProxy(string host, int port)
        {
            try
            {
                var sw = new System.Diagnostics.Stopwatch();
                var ch = new HttpClientHandler();
                ch.Proxy = new WebProxy(host, port);
                ch.UseProxy = true;
                var client = new HttpClient(ch);
                client.Timeout = TimeSpan.FromSeconds(20.0);
                sw.Start();
                var responseString = await client.GetStringAsync(new Uri("http://google.com/"));
                sw.Stop();
                if (!string.IsNullOrEmpty(responseString))
                {
                    string mes = String.Format("有効なプロキシサーバーです > http://google.com/ {0} s", sw.Elapsed.TotalSeconds);
                    mLogger.Write("MainForm", mes);
                    return true;
                }
                else
                {
                    mLogger.Write("MainForm", "無効なプロキシサーバーです");
                    return false;
                }
            }
            catch (TaskCanceledException)
            {
                mLogger.Write("MainForm", "プロキシサーバーを用いた接続がタイムアウトしました");
                return false;
            }
            catch
            {
                mLogger.Write("MainForm", "無効なプロキシサーバーです");
                return false;
            }
        }

        private async Task<bool> IsValidInputs()
        {
            string errorMessage = "";
            Regex urlEx = new Regex(@"^(https?|ftp)://[\w/:%#\$&\?\(\)~\.=\+\-]+$", RegexOptions.IgnoreCase);
            if (!urlEx.Match(comboBox1.Text).Success)
                errorMessage += "URL を正しく入力してください\n";

            Regex dirEx = new Regex(@"[a-zA-Z]:\\.*");
            if (!dirEx.Match(textBox5.Text).Success)
                errorMessage += "保存先を正しく入力してください\n";

            if ((checkBox21.Checked && comboBox2.Text.Length == 0) || 
                (checkBox22.Checked && comboBox5.Text.Length == 0) ||
                (checkBox23.Checked && comboBox4.Text.Length == 0) ||
                (checkBox24.Checked && comboBox3.Text.Length == 0))
                errorMessage += "キーワードを入力してください\n";

            if (checkBox25.Checked)
            {
                int port = 0;
                if (!int.TryParse(textBox3.Text, out port))
                    errorMessage += "プロキシサーバーを正しく入力してください\n";
                else if (!await IsValidProxy(textBox1.Text, port))
                    errorMessage += "プロキシサーバーを正しく入力してください\n";
            }

            if (errorMessage.Length != 0)
            {
                MessageBox.Show(errorMessage, "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private string[] PickImageFormats()
        {
            List<string> format = new List<string>();

            if (checkBox1.Checked)
                format.AddRange(new string[] { "jpg", "jpeg" });
            if (checkBox2.Checked)
                format.Add("png");
            if (checkBox3.Checked)
                format.Add("bmp");
            if (checkBox4.Checked)
                format.Add("gif");

            return format.ToArray();
        }

        private void SwitchControls(bool enabled)
        {
            button1.Enabled = enabled;
            comboBox1.Enabled = enabled;
            tabPage1.Enabled = enabled;
            tabPage2.Enabled = enabled;
            tabPage3.Enabled = enabled;
            tabPage7.Enabled = enabled;
            foreach (ToolStripMenuItem ddi in View_ToolStripMenuItem.DropDownItems)
                ddi.Enabled = enabled;
            foreach (ToolStripMenuItem ddi in Plugins_ToolStripMenuItem.DropDownItems)
                ddi.Enabled = enabled;
        }

        private void ReverseControls()
        {
            if (button1.Text == "この条件でダウンロード開始")
                button1.Text = "一時停止";
            else
                button1.Text = "この条件でダウンロード開始";
            comboBox1.Enabled = !comboBox1.Enabled;
            button2.Enabled = !button2.Enabled;
            tabPage1.Enabled = !tabPage1.Enabled;
            tabPage2.Enabled = !tabPage2.Enabled;
            tabPage3.Enabled = !tabPage3.Enabled;
            tabPage7.Enabled = !tabPage7.Enabled;
            foreach (ToolStripMenuItem ddi in View_ToolStripMenuItem.DropDownItems)
                ddi.Enabled = !ddi.Enabled;
            foreach (ToolStripMenuItem ddi in Plugins_ToolStripMenuItem.DropDownItems)
                ddi.Enabled = !ddi.Enabled;
        }

        public void UpdateStatus(Status sumStatus)
        {
            toolStripStatusLabel2.Text = String.Format("完了: {0} 階層, {1} ページ, {2} 枚, {3} KB",
                sumStatus.Depth, sumStatus.Pages, sumStatus.Images, sumStatus.Size);
        }

        public void InitProgress(string title, string url, int max)
        {
            if (title != null && max > 0)
            {
                if (!listViewEx1.Items.Cast<ListViewItem>().Any(x => (string)x.Tag == url))
                {
                    ListViewItem lvi = new ListViewItem(new string[] { title, "", "1" });
                    lvi.ToolTipText = url;
                    lvi.Tag = url;
                    listViewEx1.Items.Add(lvi);

                    int idx = listViewEx1.Items.Count - 1;
                    ProgressBar pb = new ProgressBar();
                    pb.Maximum = max;
                    pb.Value = 1;
                    // Embed the ProgressBar in Column 2
                    listViewEx1.AddEmbeddedControl(pb, 1, idx);
                    listViewEx1.EnsureVisible(idx);
                }
            }
            else
                listViewEx1.Items.Clear();
        }

        public void UpdateProgress(int downloadCount, int imageCount)
        {
            int idx = listViewEx1.Items.Count - 1;
            listViewEx1.Items[idx].SubItems[2].Text = downloadCount.ToString();
            ProgressBar pb = listViewEx1.GetEmbeddedControl(1, idx) as ProgressBar;
            pb.Value = imageCount;
        }

        public void FinalizeProgress()
        {
            int idx = listViewEx1.Items.Count - 1;
            ProgressBar pb = listViewEx1.GetEmbeddedControl(1, idx) as ProgressBar;
            pb.Value = pb.Maximum;
        }

        public void UpdateImageInfo(ImageInfo info)
        {
            mUrlCache.Add(info);
            mInfoViewItems.Add(info);
        }

        public ImageInfo FindParentUrl(string url)
        {
            foreach (var info in mInfoViewItems)
            {
                if (info.ParentUrl == url)
                    return info;
            }
            return null;
        }

        public void DeleteSelectedImages(string url)
        {
            foreach(var info in mInfoViewItems)
            {
                if (info.ParentUrl == url)
                {
                    string dir = Path.GetDirectoryName(info.ImagePath);
                    if (File.Exists(info.ImagePath))
                    {
                        File.Delete(info.ImagePath);
                        Utilities.Common.DeleteEmptyDirectory(dir);
                    }
                }
            }
        }

        private void LoadPlugins()
        {
            PluginInfo[] pis = PluginInfo.FindPlugins();
            mPlugins = new Plugins.IPlugin[pis.Length];

            for (int i = 0; i < mPlugins.Length; i++)
            {
                mPlugins[i] = pis[i].CreateInstance();
                mPlugins[i].Logger = mLogger;
                ToolStripMenuItem mi = new ToolStripMenuItem(mPlugins[i].Name);
                mi.Click += new EventHandler(menuPlugin_Click);
                Plugins_ToolStripMenuItem.DropDownItems.Add(mi);
            }
        }

        private void SerializeFormSettings()
        {
            FormSettings settings = new FormSettings();
            settings.LoggerFormEnabled = LoggerFormEnabled_ToolStripMenuItem.Checked;
            settings.Properties = Utilities.ControlProperty.Get(this.Controls);

            for (int i = comboBox1.Items.Count - 1; i >= 0; i--)
                settings.UrlList.Insert(0, comboBox1.Items[i].ToString());
            for (int i = comboBox2.Items.Count - 1; i >= 0; i--)
                settings.KeyTitleList.Insert(0, comboBox2.Items[i].ToString());
            for (int i = comboBox5.Items.Count - 1; i >= 0; i--)
                settings.ExKeyTitleList.Insert(0, comboBox5.Items[i].ToString());
            for (int i = comboBox3.Items.Count - 1; i >= 0; i--)
                settings.KeyUrlList.Insert(0, comboBox3.Items[i].ToString());
            for (int i = comboBox4.Items.Count - 1; i >= 0; i--)
                settings.ExKeyUrlList.Insert(0, comboBox4.Items[i].ToString());
            for (int i = comboBox9.Items.Count - 1; i >= 0; i--)
                settings.KeyRootUrlList.Insert(0, comboBox9.Items[i].ToString());
            for (int i = comboBox8.Items.Count - 1; i >= 0; i--)
                settings.ExKeyRootUrlList.Insert(0, comboBox8.Items[i].ToString());
            for (int i = comboBox7.Items.Count - 1; i >= 0; i--)
                settings.KeyImageUrlList.Insert(0, comboBox7.Items[i].ToString());
            for (int i = comboBox6.Items.Count - 1; i >= 0; i--)
                settings.ExKeyImageUrlList.Insert(0, comboBox6.Items[i].ToString());

            var xs = new XmlSerializer(typeof(FormSettings));
            using (var sw = new StreamWriter("ImageScraper.xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, settings);
        }

        private void SaveSettings()
        {
            try
            {
                SerializeFormSettings();
                mLogger.Write("MainForm", "フォームの設定を保存しました");
            }
            catch
            {
                mLogger.Write("MainForm", "フォームの設定を正常に保存できませんでした");
            }

            foreach (var plugin in mPlugins)
            {
                try
                {
                    plugin.SaveSettings();
                    mLogger.Write(plugin.Name, "プラグインの設定を保存しました");
                }
                catch
                {
                    mLogger.Write(plugin.Name, "プラグインの設定を正常に保存できませんでした");
                }
            }

            try
            {
                var xs = new XmlSerializer(typeof(List<ImageInfo>));
                var urlCache = mUrlCache.ToList();
                using (var sw = new StreamWriter("UrlCache.xml", false, new UTF8Encoding(false)))
                    xs.Serialize(sw, urlCache);
                mLogger.Write("MainForm", "履歴を保存しました");
            }
            catch
            {
                mLogger.Write("MainForm", "履歴を正常に保存できませんでした");
            }
        }

        private void DeserializeFormSettings()
        {
            using (var sr = new StreamReader("ImageScraper.xml", new UTF8Encoding(false)))
            {
                var xs = new XmlSerializer(typeof(FormSettings));
                var settings = xs.Deserialize(sr) as FormSettings;

                if (settings.UrlList != null)
                    comboBox1.Items.AddRange(settings.UrlList.ToArray());
                if (settings.KeyTitleList != null)
                    comboBox2.Items.AddRange(settings.KeyTitleList.ToArray());
                if (settings.ExKeyTitleList != null)
                    comboBox5.Items.AddRange(settings.ExKeyTitleList.ToArray());
                if (settings.KeyUrlList != null)
                    comboBox3.Items.AddRange(settings.KeyUrlList.ToArray());
                if (settings.ExKeyUrlList != null)
                    comboBox4.Items.AddRange(settings.ExKeyUrlList.ToArray());
                if (settings.KeyRootUrlList != null)
                    comboBox9.Items.AddRange(settings.KeyRootUrlList.ToArray());
                if (settings.ExKeyRootUrlList != null)
                    comboBox8.Items.AddRange(settings.ExKeyRootUrlList.ToArray());
                if (settings.KeyImageUrlList != null)
                    comboBox7.Items.AddRange(settings.KeyImageUrlList.ToArray());
                if (settings.ExKeyImageUrlList != null)
                    comboBox6.Items.AddRange(settings.ExKeyImageUrlList.ToArray());

                Utilities.ControlProperty.Set(this.Controls, settings.Properties);
                LoggerFormEnabled_ToolStripMenuItem.Checked = settings.LoggerFormEnabled;
            }
        }

        private void DeserializeIncompatibleUrlCache()
        {
            var xs = new XmlSerializer(typeof(List<Utilities.Common.KeyAndValue<string, ImageInfo>>));
            using (var sr = new StreamReader("UrlCache.xml", new UTF8Encoding(false)))
            {
                var urlCache = (List<Utilities.Common.KeyAndValue<string, ImageInfo>>)xs.Deserialize(sr);
                var urlCacheDict = Utilities.Common.ConvertListToDictionary(urlCache);
                mUrlCache = new HashSet<ImageInfo>();
                foreach (var pair in urlCacheDict)
                {
                    var info = new ImageInfo();
                    info.ImagePath = pair.Value.ImagePath;
                    info.ImageUrl = pair.Key;
                    info.TimeStamp = pair.Value.TimeStamp;
                    info.ParentTitle = pair.Value.ParentTitle;
                    info.ParentUrl = pair.Value.ParentUrl;
                    mUrlCache.Add(info);
                }
            }
        }

        private void LoadSettings()
        {
            if (File.Exists("ImageScraper.xml"))
            {
                try
                {
                    DeserializeFormSettings();
                    mLogger.Write("MainForm", "フォームの設定を読み込みました");
                }
                catch
                {
                    mLogger.Write("MainForm", "フォームの設定を正常に読み込めませんでした");
                }
            }

            foreach (var plugin in mPlugins)
            {
                try
                {
                    plugin.LoadSettings();
                    mLogger.Write(plugin.Name, "プラグインの設定を読み込みました");
                }
                catch
                {
                    mLogger.Write(plugin.Name, "プラグインの設定を正常に読み込めませんでした");
                }
            }

            if (File.Exists("UrlCache.xml"))
            {
                try
                {
                    var xs = new XmlSerializer(typeof(List<ImageInfo>));
                    using (var sr = new StreamReader("UrlCache.xml", new UTF8Encoding(false)))
                    {
                        var urlCache = xs.Deserialize(sr) as List<ImageInfo>;
                        mUrlCache = new HashSet<ImageInfo>(urlCache);
                    }
                    mLogger.Write("MainForm", "履歴を読み込みました");
                }
                catch
                {
                    DeserializeIncompatibleUrlCache();
                    mLogger.Write("MainForm", "旧バージョンの履歴を読み込みました");
                }
            }
        }
    }
}
