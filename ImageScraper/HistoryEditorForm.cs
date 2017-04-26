using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;

namespace ImageScraper
{
    public partial class HistoryEditorForm : Form
    {
        HashSet<ImageInfo> _removedUrlSet;
        HashSet<ImageInfo> _urlCache;
        ListViewItemComparer listViewItemSorter = new ListViewItemComparer();

        public HistoryEditorForm(HashSet<ImageInfo> urlCache)
        {
            InitializeComponent();
            this._urlCache = urlCache;
            this._removedUrlSet = new HashSet<ImageInfo>();
            ReloadHistory();
        }

        private void ReloadHistory()
        {
            int i = 0;
            var lvItems = new ListViewItem[this._urlCache.Count];
            foreach (var info in this._urlCache)
            {
                var lvi = new ListViewItem(
                    new string[] {
                        Path.GetFileName(info.ImagePath),
                        info.ParentTitle,
                        info.LoadDate.ToString("yyyy/MM/dd/ HH:mm:ss")
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
                _removedUrlSet.Add(info);
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
            foreach (var info in _removedUrlSet)
                _urlCache.Remove(info);
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

        ProgressForm GetProgressForm(string titleText, int max)
        {
            ProgressForm progressForm = new ProgressForm(titleText, max);
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
                Common.OpenFile(info.ImagePath);
        }
    }

    /// <summary>
    /// ListViewの項目の並び替えに使用するクラス
    /// </summary>
    public class ListViewItemComparer : IComparer
    {
        private int _column;
        private SortOrder _order;

        /// <summary>
        /// 並び替えるListView列の番号
        /// </summary>
        public int Column
        {
            set
            {
                //現在と同じ列の時は、昇順降順を切り替える
                if (_column == value)
                {
                    if (_order == SortOrder.Ascending)
                        _order = SortOrder.Descending;
                    else if (_order == SortOrder.Descending)
                        _order = SortOrder.Ascending;
                }
                _column = value;
            }
            get
            {
                return _column;
            }
        }
        /// <summary>
        /// 昇順か降順か
        /// </summary>
        public SortOrder Order
        {
            set
            {
                _order = value;
            }
            get
            {
                return _order;
            }
        }

        /// <summary>
        /// ListViewItemComparerクラスのコンストラクタ
        /// </summary>
        /// <param name="col">並び替える列番号</param>
        public ListViewItemComparer(int col, SortOrder ord)
        {
            _column = col;
            _order = ord;
        }
        public ListViewItemComparer()
        {
            _column = 2;
            _order = SortOrder.Descending;
        }

        // xがyより小さいときはマイナスの数、大きいときはプラスの数、
        // 同じときは0を返す
        public int Compare(object x, object y)
        {
            // ListViewItemの取得
            ListViewItem itemx = (ListViewItem)x;
            ListViewItem itemy = (ListViewItem)y;

            // xとyを文字列として比較する
            int result = String.Compare(itemx.SubItems[_column].Text, itemy.SubItems[_column].Text);
            if (_order == SortOrder.Descending)
                result = -result;

            return result;
        }
    }
}
