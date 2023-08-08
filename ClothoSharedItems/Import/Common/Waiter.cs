using System;
using System.Threading;

namespace ClothoSharedItems.Common
{
    public static class Waiter
    {
        public static void Doze()
        {
            Thread.Sleep(0);
        }

        public static void Wait(double timeout)
        {
            if (timeout > 0) Thread.Sleep((int)(timeout * 1000));
        }

        public static bool Wait(double timeout, ref bool flagRun, double checkInterval = 0.1)
        {
            DateTime dtStart = DateTime.Now;
            if (timeout > 0)
            {
                int interval_ms = (int)(checkInterval * 1000);
                double leftTime = timeout;
                while (leftTime > 0)
                {
                    if (!flagRun) break;

                    if (leftTime > checkInterval)
                        Thread.Sleep(interval_ms);
                    else
                    {
                        Thread.Sleep((int)(leftTime * 1000));
                        break;
                    }
                    leftTime = timeout - (DateTime.Now - dtStart).TotalSeconds;
                }
            }
            return flagRun;
        }
    }
}