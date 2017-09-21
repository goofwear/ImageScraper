using System.Windows.Forms;

namespace ImageScraper.Plugins.CookieEditor
{
    public partial class PluginForm : Form
    {
        Editor mParent;

        public PluginForm(Editor parent)
        {
            InitializeComponent();
            mParent = parent;
        }

        public void SetCookie(string name, string value, string path, string domain)
        {
            textBox1.Text = name;
            textBox2.Text = value;
            textBox3.Text = path;
            textBox4.Text = domain;
        }

        private void PluginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mParent.SetCookie(textBox1.Text, textBox2.Text, textBox3.Text, textBox4.Text);
        }
    }
}
