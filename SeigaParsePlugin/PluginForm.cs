using System.Windows.Forms;

namespace SeigaParsePlugin
{
    public partial class PluginForm : Form
    {
        Parser mParent;

        public PluginForm(Parser parent)
        {
            InitializeComponent();
            mParent = parent;
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
            checkBox1.Checked = mParent.Enabled;
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
            if (mParent.Login(textBox1.Text, textBox2.Text))
            {
                MessageBox.Show("niconicoのログインに成功しました", "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("niconicoのログインに失敗しました", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PluginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mParent.SetAccount(textBox1.Text, textBox2.Text);
            mParent.SetEnabled(checkBox1.Checked);
        }
    }
}
