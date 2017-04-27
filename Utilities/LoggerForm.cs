using System.Windows.Forms;
using System.Collections.Generic;

namespace Utilities
{ 
    public partial class LoggerForm : Form
    {
        public LoggerForm(List<Log> logList = null)
        {
            InitializeComponent();

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
    }
}
