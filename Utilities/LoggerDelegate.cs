namespace Utilities
{
    public class LoggerDelegate
    {
        System.Windows.Forms.Control mFormsControl;

        // メソッド用デリゲート
        public delegate void Delegate_WriteLog(object sender, string module, string desc);

        // イベントの定義
        public event Delegate_WriteLog Event_WriteLog;

        public LoggerDelegate(System.Windows.Forms.Control formsControl)
        {
            mFormsControl = formsControl;
        }

        public void Write(string module, string desc)
        {
            if (Event_WriteLog != null)
                mFormsControl.Invoke(Event_WriteLog, this, module, desc);
        }
    }
}
