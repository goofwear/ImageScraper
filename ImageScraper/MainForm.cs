using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Utilities;

namespace ImageScraper
{
    public partial class MainForm : Form
    {
        const string versionString = "2.6";
        private Downloader downloader;
        private PluginInterface[] plugins;
        private DownloadSettings downloadSettings = new DownloadSettings();
        private FormSettings fromSettings = new FormSettings();
        private HashSet<ImageInfo> urlCache = new HashSet<ImageInfo>();
        private HashSet<ImageInfo> infoViewItems = new HashSet<ImageInfo>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                this.LoadPlugins();
                this.LoadSettings();
            }
            catch
            {
                MessageBox.Show("設定ファイルの読み込みに失敗しました", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                groupBox8.Enabled = checkBox12.Checked;
                groupBox11.Enabled = checkBox11.Checked;
                this.FormClosing += this.Form1_FormClosing;
            }
        }

        private void menuPlugin_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = (ToolStripMenuItem)sender;
            foreach(PluginInterface plugin in plugins)
            {
                if (mi.Text == plugin.Name)
                {
                    plugin.ShowPluginForm();
                    break;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                this.SaveSettings();
            }
            catch
            {
                MessageBox.Show("設定ファイルの書き込みに失敗しました", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (downloader != null)
            {
                if (downloader.SuspendTask())
                    button1.Text = "再開";
                else if (downloader.ResumeTask())
                    button1.Text = "一時停止";
            }
            else
                this.RunDownloader();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (downloader != null)
            {
                downloader.ResumeTask();
                toolStripStatusLabel1.Text = "最終処理中...";
                downloader.StopTask();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //メッセージボックスを表示する
            var res = MessageBox.Show( "ダウンロード履歴を削除します\nよろしいですか？", "確認", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);

            //何が選択されたか調べる
            if (res == DialogResult.Yes)
                this.urlCache.Clear();
        }

        private void comboBox1_DragEnter(object sender, DragEventArgs e)
        {
            //URLのみ受け入れる
            if (e.Data.GetDataPresent("UniformResourceLocator"))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void comboBox1_DragDrop(object sender, DragEventArgs e)
        {
            //ドロップされたリンクのURLを取得する
            string url = e.Data.GetData(DataFormats.Text).ToString();
            //結果を表示
            comboBox1.Text = url;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var dr = folderBrowserDialog1.ShowDialog();
            if (dr == DialogResult.OK)
                textBox5.Text = folderBrowserDialog1.SelectedPath;
        }

        private void CopyTitle_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedIndices.Count == 1)
            {
                int idx = listViewEx1.SelectedIndices[0];
                Clipboard.SetDataObject(listViewEx1.Items[idx].SubItems[0].Text);
            }
        }

        private void CopyUrl_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedIndices.Count == 1)
            {
                int idx = listViewEx1.SelectedIndices[0];
                Clipboard.SetDataObject(listViewEx1.Items[idx].Tag);
            }
        }

        private void OpenUrl_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedIndices.Count == 1)
            {
                int idx = listViewEx1.SelectedIndices[0];
                System.Diagnostics.Process.Start(listViewEx1.Items[idx].Tag.ToString());
            }
        }

        private void listViewEx1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem lvi = listViewEx1.GetItemAt(e.X, e.Y);
            if (lvi != null)
                System.Diagnostics.Process.Start(lvi.Tag.ToString());
        }

        private void About_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string str = String.Format(
                "プログラム名:\n    ImageScraper {0}\n" +
                "ホームページ:\n    http://ux.getuploader.com/csharp_scraping/",
                versionString);
            MessageBox.Show(str, "ImageScraperについて",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteItem_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count > 0)
            {
                int num = 0;
                for (int i = 0; i < listViewEx1.SelectedItems.Count; i++)
                {
                    var url = listViewEx1.SelectedItems[i].Tag.ToString();
                    foreach (var info in infoViewItems)
                    {
                        if (info.ParentUrl == url)
                            num++;
                    }
                }
                //メッセージボックスを表示する
                DialogResult res = MessageBox.Show(String.Format("{0}枚の画像を削除します\nよろしいですか？", num), "確認",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);

                //何が選択されたか調べる
                if (res == DialogResult.Yes)
                {
                    for (int i = listViewEx1.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        int idx = listViewEx1.SelectedItems[i].Index;
                        DeleteSelectedImages(listViewEx1.Items[idx].Tag.ToString());
                        ProgressBar pb = listViewEx1.GetEmbeddedControl(1, idx) as ProgressBar;
                        listViewEx1.RemoveEmbeddedControl(pb);
                        listViewEx1.Items[idx].Remove();
                    }
                }
            }
        }

        private void OpenDirectory_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count > 0)
            {
                int idx = listViewEx1.SelectedItems[0].Index;
                ImageInfo imageInfo = GetInitializedImageInfo(listViewEx1.Items[idx].Tag.ToString());
                Common.OpenExplorer(imageInfo.ImagePath);
            }
        }

        private void EnabledLoggerForm_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnabledLoggerForm_ToolStripMenuItem.Checked = !EnabledLoggerForm_ToolStripMenuItem.Checked;
            if (EnabledLoggerForm_ToolStripMenuItem.Checked)
                ShowLoggerForm();
            else if (mLoggerForm != null && !mLoggerForm.IsDisposed)
                mLoggerForm.Dispose();
        }

        private void LoggerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            EnabledLoggerForm_ToolStripMenuItem.Checked = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var f = new HistoryEditorForm(this.urlCache);
            f.ShowDialog(this);
            f.Dispose();
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            groupBox11.Enabled = checkBox11.Checked;
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            groupBox8.Enabled = checkBox12.Checked;
        }

        private void SelectAll_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listViewEx1.Focus();
            listViewEx1.BeginUpdate();
            for (int i = 0; i < listViewEx1.Items.Count; i++)
                listViewEx1.Items[i].Selected = true;
            listViewEx1.EndUpdate();
        }
    }
}
