using System.Windows.Forms;

namespace Utilities
{
    public class LoggerDelegate
    {
        Control mParent;

        // メソッド用デリゲート
        public delegate void Delegate_WriteLog(object sender, string module, string desc);

        // イベントの定義
        public event Delegate_WriteLog Event_WriteLog;

        public LoggerDelegate(Control parent)
        {
            mParent = parent;
        }

        public void Write(string module, string desc)
        {
            if (Event_WriteLog != null)
                mParent.Invoke(Event_WriteLog, this, module, desc);
        }
    }
}
