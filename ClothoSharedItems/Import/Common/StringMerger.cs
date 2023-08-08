using System.Collections.Generic;
using System.Text;

namespace ClothoSharedItems.Common
{
    public sealed class StringMerger
    {
        private List<string> m_strVals = new List<string>();

        public void Reset()
        {
            m_strVals.Clear();
        }

        public int Count { get { return m_strVals.Count; } }

        public void AddNext(string value)
        {
            m_strVals.Add(value);
        }

        public override string ToString()
        {
            return this.ToString(",");
        }

        public string ToString(string separator)
        {
            if (m_strVals.Count == 0)
                return string.Empty;
            else if (m_strVals.Count == 1)
                return m_strVals[0];
            else
            {
                StringBuilder builder = new StringBuilder();
                foreach (var value in m_strVals)
                {
                    if (builder.Length > 0) builder.Append(separator);
                    builder.Append(value);
                }
                return builder.ToString();
            }
        }
    }
}