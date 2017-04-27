using System.Windows.Forms;

namespace PixivParsePlugin
{
    public partial class PluginForm : Form
    {
        public Parser Host { get; set; }

        public PluginForm()
        {
            InitializeComponent();
        }

        internal void SetAccount(Account userAccount)
        {
            if (userAccount != null)
            {
                textBox1.Text = userAccount.Id;
                textBox2.Text = userAccount.Pass;
            }
        }

        internal void SetEnabled()
        {
            checkBox1.Checked = Host.Enabled;
        }

        internal void SetFormEnabled(bool enabled)
        {
            checkBox1.Enabled = enabled;
            groupBox1.Enabled = enabled;
        }

        internal Account GetAccount()
        {
            return new Account(textBox1.Text, textBox2.Text);
        }

        internal bool GetEnabled()
        {
            return checkBox1.Checked;
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (Host.Login(textBox1.Text, textBox2.Text))
            {
                MessageBox.Show("pixivのログインに成功しました", "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("pixivのログインに失敗しました", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PluginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Host.SetAccount(textBox1.Text, textBox2.Text);
            Host.SetEnabled(checkBox1.Checked);
        }
    }
}
