using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageScraper
{
    public partial class HistoryEditorForm : Form
    {
        HashSet<ImageInfo> mRemovedUrlSet;
        HashSet<ImageInfo> mUrlCache;
        Utilities.ListViewItemComparer listViewItemSorter = new Utilities.ListViewItemComparer();

        public HistoryEditorForm(HashSet<ImageInfo> urlCache)
        {
            InitializeComponent();
            this.mUrlCache = urlCache;
            this.mRemovedUrlSet = new HashSet<ImageInfo>();
            ReloadHistory();
        }

        private void ReloadHistory()
        {
            int i = 0;
            var lvItems = new ListViewItem[this.mUrlCache.Count];
            foreach (var info in this.mUrlCache)
            {
                var lvi = new ListViewItem(
                    new string[] {
                        Path.GetFileName(info.ImagePath),
                        info.ParentTitle,
                        info.TimeStamp.ToString("yyyy/MM/dd/ HH:mm:ss")
                    }
                );
                if (!File.Exists(info.ImagePath))
                    lvi.ForeColor = Color.Red;
                lvi.Tag = info;
                lvItems[i] = lvi;
                i++;
            }
            listViewEx1.BeginUpdate();
            listViewEx1.Items.Clear();
            listViewEx1.Items.AddRange(lvItems);
            listViewEx1.EndUpdate();
            listViewEx1.ListViewItemSorter = listViewItemSorter;
            UpdateRowColor();
        }

        private void UpdateRowColor()
        {
            string previousUrl = null;
            Color[] backColors = { Color.White, Color.LightGray };
            bool flag = false;
            foreach (ListViewItem lvi in listViewEx1.Items)
            {
                string parentUrl = ((ImageInfo)(lvi.Tag)).ParentUrl;
                if (parentUrl != previousUrl)
                    flag = !flag;
                if (flag)
                    lvi.BackColor = backColors[0];
                else
                    lvi.BackColor = backColors[1];
                previousUrl = parentUrl;
            }
        }

        private void listViewEx1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            listViewItemSorter.Column = e.Column;
            listViewEx1.Sort();
            UpdateRowColor();
        }

        private void DeleteHistory()
        {
            if (listViewEx1.SelectedItems.Count == 0)
                return;

            listViewEx1.BeginUpdate();
            for (int i = listViewEx1.SelectedItems.Count - 1; i >= 0; i--)
            {
                // 順番注意
                var info = listViewEx1.SelectedItems[i].Tag as ImageInfo;
                mRemovedUrlSet.Add(info);
                listViewEx1.Items.RemoveAt(listViewEx1.SelectedItems[i].Index);
            }
            listViewEx1.EndUpdate();
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            DeleteHistory();
        }

        private void MenuDelete_Click(object sender, EventArgs e)
        {
            DeleteHistory();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (var info in mRemovedUrlSet)
                mUrlCache.Remove(info);
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {
            listViewEx1.Focus();
            listViewEx1.BeginUpdate();
            for (int i = 0; i < listViewEx1.Items.Count; i++)
                listViewEx1.Items[i].Selected = true;
            listViewEx1.EndUpdate();
        }

        private void listViewEx1_VisibleChanged(object sender, EventArgs e)
        {
            UpdateRowColor();
        }

        Utilities.ProgressForm GetProgressForm(string titleText, int max)
        {
            var progressForm = new Utilities.ProgressForm(titleText, max);
            progressForm.Owner = this;
            progressForm.Left = this.Left + (this.Width - progressForm.Width) / 2;
            progressForm.Top = this.Top + (this.Height - progressForm.Height) / 2;

            return progressForm;
        }

        private async void DownloadWebImages()
        {
            if (listViewEx1.SelectedItems.Count == 0)
                return;

            try
            {
                var cookies = new CookieContainer();
                var urlList = new List<ImageInfo>();
                foreach (ListViewItem lvi in listViewEx1.SelectedItems)
                {
                    var info = lvi.Tag as ImageInfo;
                    if (!File.Exists(info.ImagePath))
                    {
                        string imageDir = Path.GetDirectoryName(info.ImagePath);
                        if (!Directory.Exists(imageDir))
                            Directory.CreateDirectory(imageDir);
                        urlList.Add(info);
                    }
                }
                using (var progressForm = GetProgressForm("ダウンロード中", urlList.Count))
                {
                    progressForm.Show();
                    await Task.Run(() =>
                    {
                        foreach (var info in urlList)
                        {
                            var uc = new UrlContainer.UrlContainer(info.ImageUrl);
                            uc.Referer = info.ParentUrl;
                            uc.Download(info.ImagePath, cookies);

                            if (progressForm.isCancelled)
                                throw new OperationCanceledException();
                            else
                                progressForm.IncrementProgressBar();
                        }
                    });
                }
                MessageBox.Show("ダウンロードが完了しました", "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("ダウンロードはキャンセルされました", "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("ダウンロードに失敗しました", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ReloadHistory();
            }
        }

        private void Download_Click(object sender, EventArgs e)
        {
            DownloadWebImages();
        }

        private void MenuDownload_Click(object sender, EventArgs e)
        {
            DownloadWebImages();
        }

        private void MenuReload_Click(object sender, EventArgs e)
        {
            ReloadHistory();
        }

        private void listViewEx1_DoubleClick(object sender, EventArgs e)
        {
            var info = listViewEx1.SelectedItems[0].Tag as ImageInfo;
            if (File.Exists(info.ImagePath))
                Utilities.Common.OpenFile(info.ImagePath);
        }

        private void SelectAll_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listViewEx1.Focus();
            listViewEx1.BeginUpdate();
            for (int i = 0; i < listViewEx1.Items.Count; i++)
                listViewEx1.Items[i].Selected = true;
            listViewEx1.EndUpdate();
        }

        private void comboBox1_TextUpdate(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(comboBox1.Text))
            {
                ReloadHistory();
                return;
            }

            listViewEx1.Items.Clear();
            listViewEx1.BeginUpdate();
            foreach (var info in this.mUrlCache)
            {
                if (info.ParentTitle.ToLower().Contains(comboBox1.Text.ToLower()))
                {
                    var lvi = new ListViewItem(
                        new string[] {
                        Path.GetFileName(info.ImagePath),
                        info.ParentTitle,
                        info.TimeStamp.ToString("yyyy/MM/dd/ HH:mm:ss")
                        }
                    );
                    if (!File.Exists(info.ImagePath))
                        lvi.ForeColor = Color.Red;
                    lvi.Tag = info;
                    listViewEx1.Items.Add(lvi);
                }
            }
            listViewEx1.EndUpdate();
            UpdateRowColor();
        }
    }
}
