using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ImageScraper
{
    partial class MainForm
    {
        private string[] mAvailableFormats = new string[] { "jpg", "jpeg", "png", "bmp", "gif" };

        private void InitializeSettings(DownloadSettings dc)
        {
            // ダウンロード設定
            dc.UrlContainer = new UrlContainer.UrlContainer(comboBox1.Text);
            dc.Formats = PickImageFormats();
            dc.ParseHrefAttr = checkBox7.Checked;
            dc.ParseImgTag = checkBox8.Checked;
            dc.DomainFilter = new DomainFilter(checkBox6.Checked, dc.UrlContainer);
            dc.ColorFilter = new ColorFilter(checkBox5.Checked, checkBox20.Checked);
            dc.ImageSizeFilter = new ValueRangeFilter(
                checkBox14.Checked, 
                checkBox17.Checked, 
                (int)numericUpDown1.Value, 
                (int)numericUpDown10.Value);
            dc.ImagesPerPageFilter = new ValueRangeFilter(
                checkBox15.Checked, 
                checkBox18.Checked, 
                (int)numericUpDown2.Value, 
                (int)numericUpDown11.Value);
            dc.TitleFilter = new KeywordFilter(
                checkBox11.Checked,
                checkBox21.Checked,
                checkBox22.Checked,
                comboBox2.Text, 
                comboBox5.Text);
            dc.UrlFilter = new KeywordFilter(
                checkBox12.Checked,
                checkBox24.Checked,
                checkBox23.Checked,
                comboBox3.Text,
                comboBox4.Text);
            dc.ResolutionFilter = new ResolutionFilter(
                checkBox16.Checked, 
                checkBox19.Checked,
                (int)numericUpDown5.Value,
                (int)numericUpDown6.Value, 
                (int)numericUpDown12.Value,
                (int)numericUpDown13.Value);

            // 保存設定
            dc.RootDirectory = textBox5.Text.TrimEnd('\\') + "\\";
            dc.AppendsUrl = checkBox9.Checked;
            dc.AppendsTitle = checkBox10.Checked;
            dc.OverlappedUrlFilter = new OverlappedUrlFilter(mUrlCache, checkBox13.Checked);
            dc.FileNameGenerator = new FileNameGenerator(
                radioButton2.Checked,
                new Utilities.SerialNameGenerator(
                    textBox2.Text, 
                    (int)numericUpDown9.Value, 
                    mAvailableFormats)
            );

            // 終了条件設定
            dc.StatusMonitor = new StatusMonitor(
                new bool[] {
                    radioButton12.Checked,
                    radioButton10.Checked,
                    radioButton5.Checked,
                    radioButton6.Checked,
                    radioButton7.Checked
                },
                new Status((int)numericUpDown3.Value,
                    (int)numericUpDown8.Value,
                    (int)numericUpDown4.Value,
                    (double)numericUpDown7.Value * 1000
                ),
                (int)numericUpDown14.Value,
                this.CountImages(dc.RootDirectory)
            );

            // 接続設定
            UrlContainer.UrlContainer.RequestSpan = (int)numericUpDown15.Value;
            HtmlContainer.HtmlContainer.RequestSpan = (int)numericUpDown15.Value;

            // ロガー
            dc.Logger = mLogger;
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
            this.ReverseControls();
            toolStripStatusLabel1.Text = "ダウンロード中...";
        }

        private void FinalizeForm()
        {
            this.ReverseControls();
            toolStripStatusLabel1.Text = "完了";
        }

        async void RunDownloader()
        {
            while (true)
            {
                // 設定値の反映
                if (!this.IsValidInputs())
                    break;
                this.InitializeSettings(mDownloadSettings);

                try
                {
                    Directory.CreateDirectory(mDownloadSettings.RootDirectory);
                    for (int i = 0; i < mPlugins.Length; i++)
                        mPlugins[i].PreProcess();
                    mDownloader = new Downloader(mDownloadSettings, mPlugins, this);
                    InitializeForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }

                try
                {
                    // タスクの実行
                    await mDownloader.Start();
                }
                catch (ApplicationException ex)
                {
                    MessageBox.Show(ex.Message, "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // 後処理
                FinalizeForm();
                MessageBox.Show("ダウンロードが完了しました", "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                for (int i = 0; i < mPlugins.Length; i++)
                    mPlugins[i].PostProcess();
                mDownloader = null;

                break;
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

        private bool IsValidInputs()
        {
            string errorMessage = "";
            Regex urlEx = new Regex(@"^(https?|ftp)://[\w/:%#\$&\?\(\)~\.=\+\-]+$", RegexOptions.IgnoreCase);
            if (!urlEx.Match(comboBox1.Text).Success)
                errorMessage += "URLを入力してください\n";

            Regex dirEx = new Regex(@"[a-zA-Z]:\\.*");
            if (!dirEx.Match(textBox5.Text).Success)
                errorMessage += "保存先を入力してください\n";

            if ((checkBox21.Checked && comboBox2.Text.Length == 0) || (checkBox22.Checked && comboBox5.Text.Length == 0) ||
                (checkBox23.Checked && comboBox4.Text.Length == 0) || (checkBox24.Checked && comboBox3.Text.Length == 0))
                errorMessage += "キーワードを入力してください\n";

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
            mPlugins = new Plugins.PluginInterface[pis.Length];

            for (int i = 0; i < mPlugins.Length; i++)
            {
                mPlugins[i] = pis[i].CreateInstance();
                mPlugins[i].Logger = mLogger;
                ToolStripMenuItem mi = new ToolStripMenuItem(mPlugins[i].Name);
                mi.Click += new EventHandler(menuPlugin_Click);
                Plugins_ToolStripMenuItem.DropDownItems.Add(mi);
            }
        }

        private void SaveSettings()
        {
            try
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
                // フォーム設定のシリアライズ
                var xs = new XmlSerializer(typeof(FormSettings));
                using (var sw = new StreamWriter("ImageScraper.xml", false, new UTF8Encoding(false)))
                    xs.Serialize(sw, settings);
                mLogger.Write("MainForm", "フォームの設定を保存しました");
            }
            catch
            {
                mLogger.Write("MainForm", "フォームの設定の保存に失敗しました");
            }

            // プラグイン設定の保存
            foreach (var plugin in mPlugins)
            {
                try
                {
                    plugin.SaveSettings();
                    mLogger.Write(plugin.Name, "プラグインの設定を保存しました");
                }
                catch
                {
                    mLogger.Write(plugin.Name, "プラグインの設定の保存に失敗しました");
                }
            }

            try
            {
                // 履歴のシリアライズ
                var xs = new XmlSerializer(typeof(List<ImageInfo>));
                var urlCache = mUrlCache.ToList();
                using (var sw = new StreamWriter("UrlCache.xml", false, new UTF8Encoding(false)))
                    xs.Serialize(sw, urlCache);
                mLogger.Write("MainForm", "履歴を保存しました");
            }
            catch
            {
                mLogger.Write("MainForm", "履歴の保存に失敗しました");
            }
        }

        private void LoadSettings()
        {
            if (File.Exists("ImageScraper.xml"))
            {
                try
                {
                    using (var sr = new StreamReader("ImageScraper.xml", new UTF8Encoding(false)))
                    {
                        // フォーム設定のデシリアライズ
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
                        Utilities.ControlProperty.Set(this.Controls, settings.Properties);
                        LoggerFormEnabled_ToolStripMenuItem.Checked = settings.LoggerFormEnabled;
                        mLogger.Write("MainForm", "フォームの設定を読み込みました");
                    }
                }
                catch
                {
                    mLogger.Write("MainForm", "フォームの設定の読み込みに失敗しました");
                }
            }

            // プラグイン設定の読み込み
            foreach (var plugin in mPlugins)
            {
                try
                {
                    plugin.LoadSettings();
                    mLogger.Write(plugin.Name, "プラグインの設定を読み込みました");
                }
                catch
                {
                    mLogger.Write(plugin.Name, "プラグインの設定の読み込みに失敗しました");
                }
            }

            if (File.Exists("UrlCache.xml"))
            {
                try
                {
                    // 履歴のデシリアライズ
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
                    // 旧バージョンの設定ファイル読み込み
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
                    mLogger.Write("MainForm", "旧バージョンの履歴を読み込みました");
                }
            }
        }
    }
}
