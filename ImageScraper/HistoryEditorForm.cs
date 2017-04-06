using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageScraper
{
    public partial class HistoryEditorForm : Form
    {
        HashSet<string> _removedUrlSet;
        Dictionary<string, ImageInfo> _urlCache;
        ListViewItemComparer listViewItemSorter = new ListViewItemComparer();

        public HistoryEditorForm(Dictionary<string, ImageInfo> urlCache)
        {
            InitializeComponent();
            this._urlCache = urlCache;
            this._removedUrlSet = new HashSet<string>();
            ReloadHistory();
        }

        private string GetImagePath(int index)
        {
            var imageUrl = listViewEx1.Items[index].Tag.ToString();
            return this._urlCache[imageUrl].ImagePath;
        }

        private string GetImageUrl(int index)
        {
            return listViewEx1.Items[index].Tag.ToString();
        }

        private void ReloadHistory()
        {
            int i = 0;
            var lvItems = new ListViewItem[this._urlCache.Count];
            foreach (var pair in this._urlCache)
            {
                var lvi = new ListViewItem(
                    new string[] {
                        Path.GetFileName(pair.Value.ImagePath),
                        pair.Value.ParentTitle,
                        pair.Value.LoadDate.ToString("yyyy/MM/dd/ HH:mm:ss")
                    }
                );
                if (!File.Exists(pair.Value.ImagePath))
                    lvi.ForeColor = Color.Red;
                lvi.Tag = pair.Key;
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
                string parentUrl = _urlCache[lvi.Tag.ToString()].ParentUrl;
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
                _removedUrlSet.Add(GetImageUrl(listViewEx1.SelectedIndices[i]));
                listViewEx1.Items.RemoveAt(listViewEx1.SelectedIndices[i]);
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
            foreach (var url in _removedUrlSet)
                _urlCache.Remove(url);
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
                var urlTable = new Dictionary<string, string>();
                foreach (ListViewItem lvi in listViewEx1.SelectedItems)
                {
                    string imagePath = GetImagePath(lvi.Index);
                    if (!File.Exists(imagePath))
                    {
                        string imageDir = Path.GetDirectoryName(imagePath);
                        if (!Directory.Exists(imageDir))
                            Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                        urlTable.Add(GetImageUrl(lvi.Index), imagePath);
                    }
                }
                using (var progressForm = GetProgressForm("ダウンロード中", urlTable.Count))
                {
                    progressForm.Show();
                    await Task.Run(() =>
                    {
                        foreach (var pair in urlTable)
                        {
                            var uc = new UrlContainer.UrlContainer(pair.Key);
                            uc.Download(pair.Value, cookies);

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
            var imagePath = GetImagePath(listViewEx1.SelectedIndices[0]);
            if (File.Exists(imagePath))
                Common.OpenFile(imagePath);
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
