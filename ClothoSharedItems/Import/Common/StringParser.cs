namespace ClothoSharedItems.Common
{
    public sealed class StringParser
    {
        private const string defaultSeparator = ", ";

        private int m_rdPos;
        private string[] m_strVals;

        public StringParser(string value, string separator = defaultSeparator)
        {
            m_rdPos = 0;
            m_strVals = (value == null ? new string[] { } : value.SplitToArray(separator.ToCharArray()));
        }

        public StringParser Reset()
        {
            m_rdPos = 0; return this;
        }

        public int Count { get { return m_strVals.Length; } }
        public bool EOL { get { return m_rdPos >= m_strVals.Length; } }

        public string GetNext()
        {
            return (m_rdPos < m_strVals.Length ? m_strVals[m_rdPos++] : null);
        }
    }
}