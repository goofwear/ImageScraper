using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ImageScraper.Utilities
{
    public class Logger
    {
        List<Log> mLogList;
        LoggerForm mLoggerForm;

        public Logger()
        {
            mLogList = new List<Log>();
        }

        public void ShowForm(Action<object, FormClosingEventArgs> func, Point location)
        {
            if (mLoggerForm == null || mLoggerForm.IsDisposed)
            {
                mLoggerForm = new LoggerForm(mLogList);
                mLoggerForm.Show();
                mLoggerForm.Location = location;
                mLoggerForm.FormClosing += new FormClosingEventHandler(func);
            }
            else if (!mLoggerForm.Visible)
                mLoggerForm.Visible = true;
        }

        public void HideForm()
        {
            if (mLoggerForm != null && !mLoggerForm.IsDisposed)
                mLoggerForm.Visible = false;
        }

        public void Write(string module, string desc)
        {
            var log = new Log(module, desc);
            mLogList.Add(log);
            if (mLoggerForm != null && !mLoggerForm.IsDisposed)
                mLoggerForm.Write(log);
        }

        public void WriteInvoke(string module, string desc)
        {
            var log = new Log(module, desc);
            mLogList.Add(log);
            if (mLoggerForm != null && !mLoggerForm.IsDisposed)
                mLoggerForm.Invoke(new Action(() => mLoggerForm.Write(log)));
        }
    }
}
