using System;
using System.Net;
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

        public void SetCookie(Cookie cookie)
        {
            textBox1.Text = cookie.Name;
            textBox2.Text = cookie.Value;
            textBox3.Text = cookie.Path;
            textBox4.Text = cookie.Domain;
        }

        public Cookie GetCookie()
        {
            if (!String.IsNullOrEmpty(textBox1.Text) && !String.IsNullOrEmpty(textBox4.Text))
                return new Cookie(textBox1.Text, textBox2.Text, textBox3.Text, textBox4.Text);
            else
                return new Cookie();
        }

        internal void SetEnabled()
        {
            checkBox1.Checked = mParent.Enabled;
        }

        internal void SetFormEnabled(bool enabled)
        {
            checkBox1.Enabled = enabled;
            groupBox1.Enabled = enabled;
        }

        internal bool GetEnabled()
        {
            return checkBox1.Checked;
        }

        private void PluginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mParent.SetCookie(GetCookie());
        }
    }
}
