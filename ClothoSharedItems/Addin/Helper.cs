using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace ClothoSharedItems
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
            private System.Threading.Timer _timeoutTimer;
            private string _caption;
            private DialogResult _result;
            private DialogResult _timerResult;
            private bool timedOut = false;
            private Form _message = new Form() { Size = new Size(0, 0), TopMost = true };

            private AutoClosingMessageBox(string text, string caption, int timeout, MessageBoxButtons buttons = MessageBoxButtons.OK, DialogResult timerResult = DialogResult.None, MessageBoxIcon icon = MessageBoxIcon.None)
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

            private void FormDispose()
            {
                if (_message.InvokeRequired)
                    _message.Invoke(new MethodInvoker(FormDispose));
                else
                {
                    _message.Dispose();
                }
            }

            private void OnTimerElapsed(object state)
            {
                timedOut = true;
                _timeoutTimer.Dispose();
                FormDispose();
            }
        }

        public class MakeNotifyForm
        {
            private Thread m_thread;

            private MakeNotifyForm(int timeout, string message)
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

        #endregion System_Function

        public static IEnumerable<string> GetFileList(string fileSearchPattern, string rootFolderPath)
        {
            Queue<string> pending = new Queue<string>();
            pending.Enqueue(rootFolderPath);
            string[] tmp;
            while (pending.Count > 0)
            {
                rootFolderPath = pending.Dequeue();
                try
                {
                    tmp = Directory.GetFiles(rootFolderPath, fileSearchPattern);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                for (int i = 0; i < tmp.Length; i++)
                {
                    yield return tmp[i];
                }
                tmp = Directory.GetDirectories(rootFolderPath);
                for (int i = 0; i < tmp.Length; i++)
                {
                    pending.Enqueue(tmp[i]);
                }
            }
        }

        public static List<string> Client_IP
        {
            get
            {
                List<string> cip = new List<string>();
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                string ClientIP = string.Empty;
                for (int i = 0; i < host.AddressList.Length; i++)
                {
                    if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        cip.Add(host.AddressList[i].ToString());
                    }
                }
                return cip;
            }
        }

        public class FTPClient : IDisposable
        {
            private string id = "waveform";
            private string pwd = "npi";
            private string url = "ftp://10.100.16.186:721/";
            public bool VAILDFTP { get; private set; }

            public FTPClient(string url = "ftp://10.100.16.186:721/", string id = "waveform", string pwd = "npi")
            {
                VAILDFTP = true;

                if (!url.CIvStartsWith("ftp")) url = "ftp://" + url;
                if (!url.CIvEndsWith("/")) url = url + "/";

                this.url = url;
                this.id = id;
                this.pwd = pwd;
            }

            public bool isValidConnection()
            {
                try
                {
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
                    request.Method = WebRequestMethods.Ftp.ListDirectory;
                    request.Credentials = new NetworkCredential(id, pwd);
                    request.Timeout = 1000;
                    request.GetResponse().Close();
                }
                catch (WebException ex)
                {
                    return VAILDFTP = false;
                }
                return VAILDFTP = true;
            }

            public FtpWebResponse Connect(string remoteSource, string method, Action<FtpWebRequest> action = null)
            {
                if (!VAILDFTP) return null;

                var request = WebRequest.Create(url + remoteSource) as FtpWebRequest;
                request.UseBinary = true;
                request.Method = method;
                request.Credentials = new NetworkCredential(id, pwd);
                request.Timeout = 1000;

                FtpWebResponse ftpWebResponse = null;

                try
                {
                    action?.Invoke(request);
                    ftpWebResponse = request.GetResponse() as FtpWebResponse;
                }
                catch (Exception ex)
                {
                }
                return ftpWebResponse;
            }

            public void UploadFileList(String remoteSource, string source)
            {
                if (!VAILDFTP) return;

                var attr = File.GetAttributes(source);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    DirectoryInfo dir = new DirectoryInfo(source);
                    foreach (var item in dir.GetFiles())
                    {
                        UploadFileList(url + remoteSource + "/" + item.Name, item.FullName);
                    }

                    foreach (var item in dir.GetDirectories())
                    {
                        try
                        {
                            Connect(url + remoteSource + "/" + item.Name, WebRequestMethods.Ftp.MakeDirectory).Close();
                        }
                        catch (WebException)
                        {
                        }

                        UploadFileList(url + remoteSource + "/" + item.Name, item.FullName);
                    }
                }
                else
                {
                    using (var fs = File.OpenRead(source))
                    {
                        Connect(remoteSource, WebRequestMethods.Ftp.UploadFile, (req) =>
                        {
                            req.ContentLength = fs.Length;
                            using (var stream = req.GetRequestStream())
                            {
                                fs.CopyTo(stream);
                            }
                        }).Close();
                    }
                }
            }

            public void DownloadFileList(string remoteSource, string target)
            {
                if (!VAILDFTP) return;

                var list = new List<String>();

                using (var res = Connect(remoteSource, WebRequestMethods.Ftp.ListDirectory))
                {
                    if (res == null) return;

                    using (var stream = res.GetResponseStream())
                    {
                        using (var rd = new StreamReader(stream))
                        {
                            while (true)
                            {
                                string buf = rd.ReadLine();
                                if (string.IsNullOrWhiteSpace(buf))
                                {
                                    break;
                                }
                                list.Add(buf);
                            }
                        }
                    }
                }

                foreach (var item in list)
                {
                    string filename = "";
                    try
                    {
                        var remotetarget = remoteSource + "/" + item;
                        if (item.StartsWith("/"))
                        {
                            remotetarget = remoteSource;
                            filename = Path.GetFileName(item);
                        }
                        else
                            filename = item;

                        using (var res = Connect(remotetarget, WebRequestMethods.Ftp.DownloadFile))
                        {
                            if (res == null) return;

                            using (var stream = res.GetResponseStream())
                            {
                                using (var fs = File.Create(target + "\\" + filename))
                                {
                                    stream.CopyTo(fs);
                                }
                            }
                        }
                    }
                    catch (WebException)
                    {
                        Directory.CreateDirectory(target + "\\" + item);
                        DownloadFileList(remoteSource + "/" + item, target + "\\" + item);
                    }
                }
            }

            public void Dispose()
            {
            }
        }

        public static void CheckSystemEventsHandlersForFreeze()
        {
            var handlers = typeof(SystemEvents).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var handlersValues = handlers.GetType().GetProperty("Values").GetValue(handlers);
            foreach (var invokeInfos in (handlersValues as IEnumerable).OfType<object>().ToArray())
            {
                foreach (var invokeInfo in (invokeInfos as IEnumerable).OfType<object>().ToArray())
                {
                    var syncContext = invokeInfo.GetType().GetField("_syncContext", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(invokeInfo);
                    if (syncContext == null) throw new Exception("syncContext missing");
                    if (!(syncContext is WindowsFormsSynchronizationContext)) continue;
                    var threadRef = (WeakReference)syncContext.GetType().GetField("destinationThreadRef", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(syncContext);
                    if (!threadRef.IsAlive) continue;
                    var thread = (Thread)threadRef.Target;
                    if (thread.ManagedThreadId == 1) continue;  // Change here if you have more valid UI threads to ignore
                    var dlg = (Delegate)invokeInfo.GetType().GetField("_delegate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(invokeInfo);
                    MessageBox.Show($"SystemEvents handler '{dlg.Method.DeclaringType}.{dlg.Method.Name}' could freeze app due to wrong thread: "
                                    + $"{thread.ManagedThreadId},{thread.IsThreadPoolThread},{thread.IsAlive},{thread.Name}");
                }
            }
        }

        public static void UnsubscribeuserPreferenceChanged()
        {
            MethodInfo handler = typeof(RichTextBox).GetMethod("UserPreferenceChangedHandler", BindingFlags.Instance | BindingFlags.NonPublic);

            EventInfo evt = typeof(SystemEvents).GetEvent("UserPreferenceChanged", BindingFlags.Static | BindingFlags.Public);
            MethodInfo remove = evt.GetRemoveMethod(true);

            remove.Invoke(null, new object[]
            {
                Delegate.CreateDelegate(evt.EventHandlerType, null, handler)
            });
        }

        public static void UnsubscribeSystemEvents()
        {
            try
            {
                var handlers = typeof(SystemEvents).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                var handlersValues = handlers.GetType().GetProperty("Values").GetValue(handlers);
                foreach (var invokeInfos in (handlersValues as IEnumerable).OfType<object>().ToArray())
                    foreach (var invokeInfo in (invokeInfos as IEnumerable).OfType<object>().ToArray())
                    {
                        var syncContext = invokeInfo.GetType().GetField("_syncContext", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(invokeInfo);
                        if (syncContext == null)
                            throw new Exception("syncContext missing");
                        if (!(syncContext is WindowsFormsSynchronizationContext))
                            continue;
                        var threadRef = (WeakReference)syncContext.GetType().GetField("destinationThreadRef", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(syncContext);
                        if (!threadRef.IsAlive)
                            continue;
                        var thread = (System.Threading.Thread)threadRef.Target;
                        if (thread.ManagedThreadId == 1)
                            continue;  // Change here if you have more valid UI threads to ignore
                        var dlg = (Delegate)invokeInfo.GetType().GetField("_delegate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(invokeInfo);
                        var handler = (UserPreferenceChangedEventHandler)Delegate.CreateDelegate(typeof(UserPreferenceChangedEventHandler), dlg.Target, dlg.Method.Name);
                        SystemEvents.UserPreferenceChanged -= handler;
                    }
            }
            catch
            {
            }
        }
    }
}