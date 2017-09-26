using System;
using System.Collections;
using System.Windows.Forms;

namespace ImageScraper.Utilities
{
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
