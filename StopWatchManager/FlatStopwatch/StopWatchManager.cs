using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MPAD_TestTimer
{
    /// <summary>
    /// Flat stop watch. Faster than nested.
    /// </summary>
    public class StopWatchManager
    {
        private static StopWatchManager instance;
        private bool m_isActivated;
        private bool m_isOutputDebugMessage;
        private PaStopwatchCollection2 m_model;

        private StopWatchManager()
        {
            m_isActivated = true;
            m_isOutputDebugMessage = true;
            m_model = new PaStopwatchCollection2();
        }

        public static StopWatchManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StopWatchManager();
                }
                return instance;
            }
        }

        public bool IsActivated
        {
            get { return m_isActivated; }
            set { m_isActivated = value; }
        }

        public bool IsOutputDebugMessage
        {
            get { return m_isOutputDebugMessage; }
            set { m_isOutputDebugMessage = value; }
        }

        public void Start(string name)
        {
            if (!m_isActivated) return;
            m_model.Start(name);
        }

        public void StartTest(string name, string testType)
        {
            if (!m_isActivated) return;
            m_model.StartTest(name, testType);
        }

        public void StartTest(string name, string testType, Dictionary<string, string> testConditionList)
        {
            if (!m_isActivated) return;
            m_model.StartTest(name, testType, testConditionList);
        }

        public void Start()
        {
            if (!m_isActivated) return;
            StackTrace zStackTrace = new StackTrace();
            string callingmethod = zStackTrace.GetFrame(1).GetMethod().Name;
            m_model.Start(callingmethod);
        }

        public void Start(string name, string parentName)
        {
            if (!m_isActivated) return;
            m_model.Start(name, parentName);
        }

        public void Stop(string name)
        {
            if (!m_isActivated) return;
            m_model.Stop(name);
        }

        public void Stop(string name, bool ResetTimer)
        {
            if (!m_isActivated) return;
            m_model.Stop(name);
            
        }

        public void Stop()
        {
            if (!m_isActivated) return;
            StackTrace zStackTrace = new StackTrace();
            string callingmethod = zStackTrace.GetFrame(1).GetMethod().Name;
            m_model.Stop(callingmethod);
        }


        public void Stop(string name, string parentName)
        {
            if (!m_isActivated) return;
            m_model.Stop(name, parentName);
        }

        public List<PaStopwatch2> GetList()
        {
            return m_model.GetList();
        }

        public PaStopwatch2 GetStopwatch(string name)
        {
            return m_model.GetStopwatch(name);
        }

        public string SaveToFile()
        {
            string reportPath = @"C:\Temp\StopWatchManagerOutputFile.txt";
            string header = "Insert your header description";
            return SaveToFile(reportPath, header);
        }

        public string SaveToFile(string fullPath, string headerDesc)
        {
            if (!m_isActivated)
            {
                WriteDebugLine("StopWatch manager is not active.");
                return String.Empty;
            }
            return m_model.SaveToFile(fullPath, headerDesc, '\0');
        }

        public string SaveToFile(string fullPath, string headerDesc, char delimiter)
        {
            if (!m_isActivated)
            {
                WriteDebugLine("StopWatch manager is not active.");
                return String.Empty;
            }
            return m_model.SaveToFile(fullPath, headerDesc, delimiter);
        }

        /// <summary>
        /// Clear to prepare for next execution. Run history is not cleared.
        /// </summary>
        public void Clear()
        {
            m_model.Clear();
        }

        /// <summary>
        /// Reset all history, all run. Run history is cleared.
        /// </summary>
        public void Reset()
        {
            m_model.Reset();
        }

        private void WriteDebugLine(string message)
        {
            if (!m_isOutputDebugMessage) return;
            Debug.WriteLine(message);
        }
    }

    public class L2StopWatchHelper
    {
        private string m_swParent;
        private string m_swCurrent;

        public L2StopWatchHelper(string parentName)
        {
            m_swParent = parentName;
        }

        public void Start(string name)
        {
            StopWatchManager.Instance.Start(name, m_swParent);
            m_swCurrent = name;
        }

        public void Stop()
        {
            StopWatchManager.Instance.Stop(m_swCurrent, m_swParent);
        }
    }

}