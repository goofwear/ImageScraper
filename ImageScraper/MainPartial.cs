using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ImageScraper
{
    partial class MainForm
    {
        private LoggerForm mLoggerForm;
        private string availableFormats = "jpg|jpeg|png|bmp|gif";

        private void InitializeSettings(DownloadSettings dc)
        {
            // ダウンロード設定
            dc.urlContainer = new UrlContainer.UrlContainer(comboBox1.Text);
            dc.format = this.SetDownloadImageFormat();
            dc.enabledHref = checkBox7.Checked;
            dc.enabledIsrc = checkBox8.Checked;
            dc.filterDomain = new FilterDomain(dc.urlContainer, checkBox6.Checked);
            dc.filterColorFormat = new FilterColorFormat(
                checkBox5.Checked, 
                checkBox20.Checked);
            dc.filterImageSize = new FilterValueRange(
                checkBox14.Checked, 
                checkBox17.Checked, 
                (int)numericUpDown1.Value, 
                (int)numericUpDown10.Value);
            dc.filterImageCount = new FilterValueRange(
                checkBox15.Checked, 
                checkBox18.Checked, 
                (int)numericUpDown2.Value, 
                (int)numericUpDown11.Value);
            dc.filterTitle = new FilterKeyword(
                checkBox11.Checked,
                checkBox21.Checked,
                checkBox22.Checked,
                comboBox2.Text, 
                comboBox5.Text);
            dc.filterUrl = new FilterKeyword(
                checkBox12.Checked,
                checkBox24.Checked,
                checkBox23.Checked,
                comboBox3.Text,
                comboBox4.Text);
            dc.filterResolution = new FilterResolution(
                checkBox16.Checked, 
                checkBox19.Checked,
                (int)numericUpDown5.Value,
                (int)numericUpDown6.Value, 
                (int)numericUpDown12.Value,
                (int)numericUpDown13.Value);

            //// アカウント設定
            dc.plugins = plugins;
            for (int i = 0; i < dc.plugins.Length; i++)
                dc.plugins[i].InitializePlugin();
            dc.cookies = new System.Net.CookieContainer();

            // 保存設定
            dc.dest = textBox5.Text.TrimEnd('\\') + "\\";
            dc.destPlusUrl = checkBox9.Checked;
            dc.destPlusTitle = checkBox10.Checked;
            dc.filterUrlOverlapped = new FilterUrlOverlapped(this.UrlCache);
            dc.filterUrlOverlapped.Enabled = checkBox13.Checked;
            dc.fileNameGenerator = new FileNameGenerator(
                radioButton2.Checked,
                new SerialNameGenerator(
                    textBox2.Text, 
                    (int)numericUpDown9.Value, 
                    this.availableFormats)
            );

            // 終了条件設定
            dc.checkTerminated = new CheckTerminated(
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
                this.GetDestImageCount()
            );

            // 接続設定
            UrlContainer.UrlContainer.RequestSpan = (int)numericUpDown15.Value;
            HtmlContainer.HtmlContainer.RequestSpan = (int)numericUpDown15.Value;
        }

        void ShowLoggerForm()
        {
            if (EnabledLoggerForm_ToolStripMenuItem.Checked)
            {
                mLoggerForm = new LoggerForm();
                mLoggerForm.FormClosed += new FormClosedEventHandler(LoggerForm_FormClosed);
                mLoggerForm.Show();
                mLoggerForm.Location = new Point(this.Location.X + this.Width, this.Location.Y);
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
            imageInfo.Clear();
            listViewEx1.ClearEmbeddedControl();
            listViewEx1.Items.Clear();
            UpdateComboBox(comboBox1);
            UpdateComboBox(comboBox2);
            UpdateComboBox(comboBox3);
            UpdateComboBox(comboBox4);
            UpdateComboBox(comboBox5);
            this.FlipFormControl();
            toolStripStatusLabel1.Text = "ダウンロード中...";
        }

        private void FinalizeForm()
        {
            this.FlipFormControl();
            toolStripStatusLabel1.Text = "完了";
        }

        async void RunDownloader()
        {
            while (true)
            {
                // 設定値の反映
                if (!this.CheckSettings())
                    break;
                this.InitializeSettings(this.downloadSettings);

                // 前処理
                try
                {
                    Directory.CreateDirectory(this.downloadSettings.dest);
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message, "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }

                downloader = new Downloader(this.downloadSettings, this);
                if (mLoggerForm != null)
                {
                    mLoggerForm.Clear();
                    downloader.Event_LoggerAdd += new Downloader.Delegate_LoggerAdd(mLoggerForm.Add);
                    downloader.Event_LoggerAddRange += new Downloader.Delegate_LoggerAddRange(mLoggerForm.AddRange);
                }
                downloader.Event_UpdateStatus += new Downloader.Delegate_UpdateStatus(UpdateStatus);
                downloader.Event_AddProgress += new Downloader.Delegate_AddProgress(AddProgress);
                downloader.Event_UpdateProgress += new Downloader.Delegate_UpdateProgress(UpdateProgress);
                downloader.Event_FinalizeProgress += new Downloader.Delegate_FinalizeProgress(FinalizeProgress);
                downloader.Event_UpdateImageInfo += new Downloader.Delegate_UpdateImageInfo(UpdateImageInfo);
                InitializeForm();

                try
                {
                    // タスクの実行
                    await downloader.StartTask();
                }
                catch (ApplicationException ex)
                {
                    MessageBox.Show(ex.StackTrace, "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // 後処理
                FinalizeForm();
                MessageBox.Show("ダウンロードが完了しました", "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                for (int i = 0; i < downloadSettings.plugins.Length; i++)
                    downloadSettings.plugins[i].FinalizePlugin();
                downloader = null;

                break;
            }
        }

        private int GetDestImageCount()
        {
            int imageCount = 0;
            if (Directory.Exists(this.downloadSettings.dest))
            {
                string[] formatArray = this.availableFormats.Split('|');
                for (int i = 0; i < formatArray.Length; i++)
                {
                    imageCount += Directory.GetFiles(this.downloadSettings.dest, "*." + formatArray[i], SearchOption.AllDirectories).Length;
                }
            }
            return imageCount;
        }

        private bool CheckSettings()
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

        public string[] SetDownloadImageFormat()
        {
            List<string> format = new List<string>();

            if (checkBox1.Checked) format.AddRange(new string[] { "jpg", "jpeg" });
            if (checkBox2.Checked) format.Add("png");
            if (checkBox3.Checked) format.Add("bmp");
            if (checkBox4.Checked) format.Add("gif");

            return format.ToArray();
        }

        private void FlipFormControl()
        {
            if (button1.Text == "この条件でダウンロード開始") button1.Text = "一時停止";
            else button1.Text = "この条件でダウンロード開始";
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

        private void UpdateStatus(object sender, Status sumStatus)
        {
            toolStripStatusLabel2.Text = String.Format("完了: {0} 階層, {1} ページ, {2} 枚, {3} KB",
                sumStatus.depthCount, sumStatus.pageCount, sumStatus.imageCount, sumStatus.size);
        }

        private void AddProgress(object sender, string title, string url, int max)
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
            {
                listViewEx1.Items.Clear();
            }
        }

        private void UpdateProgress(object sender, int downloadCount, int imageCount)
        {
            int idx = listViewEx1.Items.Count - 1;
            listViewEx1.Items[idx].SubItems[2].Text = downloadCount.ToString();
            ProgressBar pb = listViewEx1.GetEmbeddedControl(1, idx) as ProgressBar;
            pb.Value = imageCount;
        }

        private void FinalizeProgress(object sender)
        {
            int idx = listViewEx1.Items.Count - 1;
            ProgressBar pb = listViewEx1.GetEmbeddedControl(1, idx) as ProgressBar;
            pb.Value = pb.Maximum;
        }

        private void UpdateImageInfo(object sender, ImageInfo info)
        {
            imageInfo.Add(info);
        }

        public ImageInfo GetInitializedImageInfo(string url)
        {
            for (int i = 0; i < imageInfo.Count; i++)
            {
                if (imageInfo[i].ParentUrl == url)
                    return imageInfo[i];
            }
            return null;
        }

        public int ErectFlagsImageInfo(string url)
        {
            int imageCount = 0;
            for (int i = 0; i < imageInfo.Count; i++)
            {
                if (imageInfo[i].ParentUrl == url)
                    imageCount++;
            }
            return imageCount;
        }

        public void DeleteSelectedImages(string url)
        {
            for (int i = 0; i < imageInfo.Count; i++)
            {
                if (imageInfo[i].ParentUrl == url)
                {
                    string dir = Path.GetDirectoryName(imageInfo[i].ImagePath);
                    if (File.Exists(imageInfo[i].ImagePath))
                    {
                        File.Delete(imageInfo[i].ImagePath);
                        if (Common.IsEmptyDirectory(dir))
                            Directory.Delete(dir);
                    }
                }
            }
        }

        private void LoadPlugins()
        {
            PluginInfo[] pis = PluginInfo.FindPlugins();
            plugins = new PluginInterface.PluginInterface[pis.Length];

            for (int i = 0; i < plugins.Length; i++)
            {
                plugins[i] = pis[i].CreateInstance();
                ToolStripMenuItem mi = new ToolStripMenuItem(plugins[i].Name);
                mi.Click += new EventHandler(menuPlugin_Click);
                Plugins_ToolStripMenuItem.DropDownItems.Add(mi);
            }
        }

        private void SaveSettings()
        {
            FormSettings settings = new FormSettings();

            for (int i = comboBox1.Items.Count - 1; i >= 0; i--)
                settings.UrlList.Insert(0, comboBox1.Items[i].ToString());
            // タイトル"含む"キーワード
            for (int i = comboBox2.Items.Count - 1; i >= 0; i--)
                settings.TitleCKeywordList.Insert(0, comboBox2.Items[i].ToString());
            // タイトル"含まない"キーワード
            for (int i = comboBox5.Items.Count - 1; i >= 0; i--)
                settings.TitleNCKeywordList.Insert(0, comboBox5.Items[i].ToString());
            // URL"含む"キーワード
            for (int i = comboBox3.Items.Count - 1; i >= 0; i--)
                settings.UrlCKeywordList.Insert(0, comboBox3.Items[i].ToString());
            // URL"含まない"キーワード
            for (int i = comboBox4.Items.Count - 1; i >= 0; i--)
                settings.UrlNCKeywordList.Insert(0, comboBox4.Items[i].ToString());

            settings.Properties = ControlProperty.ControlProperty.Get(this.Controls);

            // フォーム設定のシリアライズ
            var xs = new XmlSerializer(typeof(FormSettings));
            using (var sw = new StreamWriter("ImageScraper.xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, settings);

            // 履歴のシリアライズ
            var obj = Common.ConvertDictionaryToList(this.UrlCache);
            xs = new XmlSerializer(typeof(List<Common.KeyAndValue<string, ImageInfo>>));
            using (var sw = new StreamWriter("UrlCache.xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, obj);

            foreach (var plugin in plugins)
                plugin.SaveSettings();
        }

        private void LoadSettings()
        {
            bool compatFlag = false;
            FormSettings settings = new FormSettings();
            XmlSerializer xs = new XmlSerializer(typeof(FormSettings));

            // フォーム設定のデシリアライズ
            if (File.Exists("ImageScraper.xml"))
            {
                using (var sr = new StreamReader("ImageScraper.xml", new UTF8Encoding(false)))
                {
                    try
                    {
                        settings = (FormSettings)xs.Deserialize(sr);
                    }
                    catch
                    {
                        // 旧バージョンの設定ファイル
                        compatFlag = true;
                        sr.BaseStream.Seek(0, SeekOrigin.Begin);
                        xs = new XmlSerializer(typeof(InternalSettings));
                        var compatSettings = (InternalSettings)xs.Deserialize(sr);

                        settings.UrlList = compatSettings.UrlList;
                        settings.TitleCKeywordList = compatSettings.TitleKeyList;
                        settings.UrlCKeywordList = compatSettings.UrlKeyList;
                        settings.Properties = compatSettings.Properties;
                        foreach (var url in compatSettings.ImageUrlHistory)
                            this.UrlCache.Add(url, new ImageInfo());
                    }
                }
            }

            if (!compatFlag && File.Exists("UrlCache.xml"))
            {
                // 履歴のデシリアライズ
                xs = new XmlSerializer(typeof(List<Common.KeyAndValue<string, ImageInfo>>));
                using (var sr = new StreamReader("UrlCache.xml", new UTF8Encoding(false)))
                {
                    var urlCacheList = (List<Common.KeyAndValue<string, ImageInfo>>)xs.Deserialize(sr);
                    this.UrlCache = Common.ConvertListToDictionary(urlCacheList);
                }
            }

            if (settings.UrlList != null)
                comboBox1.Items.AddRange(settings.UrlList.ToArray());
            // タイトル"含む"キーワード
            if (settings.TitleCKeywordList != null)
                comboBox2.Items.AddRange(settings.TitleCKeywordList.ToArray());
            // タイトル"含まない"キーワード
            if (settings.TitleNCKeywordList != null)
                comboBox5.Items.AddRange(settings.TitleNCKeywordList.ToArray());
            // URL"含む"キーワード
            if (settings.UrlCKeywordList != null)
                comboBox3.Items.AddRange(settings.UrlCKeywordList.ToArray());
            // URL"含まない"キーワード
            if (settings.UrlNCKeywordList != null)
                comboBox4.Items.AddRange(settings.UrlNCKeywordList.ToArray());

            ControlProperty.ControlProperty.Set(this.Controls, settings.Properties);

            foreach (var plugin in plugins)
                plugin.LoadSettings();
        }
    }
}
