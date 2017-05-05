using System.Windows.Forms;
using System.Collections.Generic;

namespace ImageScraper.Utilities
{ 
    public partial class LoggerForm : Form
    {
        Logger mParent;

        public LoggerForm(Logger parent, List<Log> logList = null)
        {
            InitializeComponent();

            mParent = parent;
            if (logList != null)
            {
                listView1.BeginUpdate();
                foreach (var log in logList)
                    listView1.Items.Add(new ListViewItem(log.ToArray()));
                listView1.EndUpdate();
            }
        }

        public void Write(Log log)
        {
            listView1.BeginUpdate();
            listView1.Items.Add(new ListViewItem(log.ToArray()));
            listView1.Items[listView1.Items.Count - 1].EnsureVisible();
            listView1.EndUpdate();
        }

        private void Clear_ToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            mParent.Clear();
            listView1.Items.Clear();
        }
    }
}
