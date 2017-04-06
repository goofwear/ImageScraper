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

        public void SetAccount(Account userAccount)
        {
            if (userAccount != null)
            {
                textBox1.Text = userAccount.Id;
                textBox2.Text = userAccount.Pass;
            }
        }

        public void SetEnabled()
        {
            checkBox1.Checked = Host.Enabled;
        }

        public void SetFormEnabled(bool enabled)
        {
            checkBox1.Enabled = enabled;
            groupBox1.Enabled = enabled;
        }

        public Account GetAccount()
        {
            return new Account(textBox1.Text, textBox2.Text);
        }

        public bool GetEnabled()
        {
            return checkBox1.Checked;
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (Host.Login(textBox1.Text, textBox2.Text))
            {
                MessageBox.Show("pixivへのログインに成功しました", "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("pixivへのログインに失敗しました", "エラー",
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
