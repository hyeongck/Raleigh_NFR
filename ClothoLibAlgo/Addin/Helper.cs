using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClothoLibAlgo
{
    public static class Helper
    {
        public static string GetDescription(Enum en)
        {
            Type type = en.GetType();
            MemberInfo[] memInfo = type.GetMember(en.ToString());
            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            return en.ToString();
        }

        public class AutoClosingMessageBox
        {
            System.Threading.Timer _timeoutTimer;
            string _caption;
            DialogResult _result;
            DialogResult _timerResult;
            bool timedOut = false;
            Form _message = new Form() { Size = new Size(0, 0), TopMost = true };

            AutoClosingMessageBox(string text, string caption, int timeout, MessageBoxButtons buttons = MessageBoxButtons.OK, DialogResult timerResult = DialogResult.None, MessageBoxIcon icon = MessageBoxIcon.None)
            {
                _caption = caption;
                _timeoutTimer = new System.Threading.Timer(OnTimerElapsed, null, timeout, System.Threading.Timeout.Infinite);
                _timerResult = timerResult;

                using (_timeoutTimer) _result = MessageBox.Show(_message, text, caption, buttons, icon);
                if (timedOut) _result = _timerResult;
            }
            public static DialogResult Show(string text, string caption, int timeout = 1000, MessageBoxButtons buttons = MessageBoxButtons.OK, DialogResult timerResult = DialogResult.None, MessageBoxIcon icon = MessageBoxIcon.None)
            {
                return new AutoClosingMessageBox(text, caption, timeout, buttons, timerResult, icon)._result;
            }
            void FormDispose()
            {
                if (_message.InvokeRequired)
                    _message.Invoke(new MethodInvoker(FormDispose));
                else
                {
                    _message.Dispose();
                }
            }
            void OnTimerElapsed(object state)
            {
                timedOut = true;
                _timeoutTimer.Dispose();
                FormDispose();
            }
        }

        public class MakeNotifyForm
        {
            private Thread m_thread;

            MakeNotifyForm(int timeout, string message)
            {
                m_thread = new Thread(() =>
                {
                    NotifyIcon notifyIcon1 = new NotifyIcon();

                    try
                    {
                        notifyIcon1.Visible = true;
                        notifyIcon1.Text = "Notify";
                        notifyIcon1.ShowBalloonTip(timeout, "HI, THERE", message, ToolTipIcon.Info);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        Thread.Sleep(timeout + 500);
                        notifyIcon1.Visible = false;
                    }
                });
                m_thread.Start();
            }

            public static void Show(string msg = "YOUR TEST is FINISHED!", int timeout = 8000)
            {
                new MakeNotifyForm(timeout, msg);
            }
        }

        public class NotifyTelegram
        {
            public static string SendMessage(string apilToken, string destID, string text)
            {
                string urlString = $"https://api.telegram.org/bot{apilToken}/sendMessage?chat_id={destID}&text={text}";
                WebClient webclient = new WebClient();

                return webclient.DownloadString(urlString);
            }
        }


        #region System_Function

        public static int GetLastProcID(string ProcName)
        {
            Process[] ProcAry = Process.GetProcessesByName(ProcName);
            int LastProcID = 0;
            DateTime LastTime = new DateTime(2000, 1, 1);

            foreach (Process Proc in ProcAry)
            {
                if (Proc.StartTime.CompareTo(LastTime) > 0)
                {
                    LastTime = Proc.StartTime;
                    LastProcID = Proc.Id;
                }
            }
            return LastProcID;
        }

        public static void KillProcByID(int ProcID)
        {
            Process Proc = Process.GetProcessById(ProcID);
            Proc.Kill();
        }

        #endregion
    }
}
