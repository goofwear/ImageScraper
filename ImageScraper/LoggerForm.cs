using System.Windows.Forms;
using System.Collections.Generic;

namespace ImageScraper
{
    public partial class LoggerForm : Form
    {
        public LoggerForm()
        {
            InitializeComponent();
        }

        public void Add(object sender, string log, int boxIndex)
        {
            if (boxIndex == 1)
            {
                listBox1.Items.Add(log);
            }
            else if (boxIndex == 2)
            {
                listBox2.Items.Add(log);
                if (listBox1.Items.Contains(log.Trim()))
                    listBox1.SelectedIndex = listBox1.Items.IndexOf(log.Trim());
                listBox2.SelectedIndex = listBox2.Items.Count - 1;
            }
            return;
        }

        public void AddRange(object sender, List<string> log, int boxIndex)
        {
            if (boxIndex == 1)
            {
                listBox1.Items.AddRange(log.ToArray());
            }
            else if (boxIndex == 2)
            {
                listBox2.Items.AddRange(log.ToArray());
                listBox2.SelectedIndex = listBox2.Items.Count - 1;
            }
            return;
        }

        public void Clear()
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();
        }

        private void ClearAll_ToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();
        }
    }
}
