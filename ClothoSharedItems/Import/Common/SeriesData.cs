using System;
using System.Collections.Generic;
using System.Linq;

namespace ClothoSharedItems.Common
{
    [Serializable]
    public sealed class SeriesData : IEnumerable<SeriesData.SeriesPoints>
    {
        public static Dictionary<PropertyID, string[]> PropertyDescriptions;

        static SeriesData()
        {
            PropertyDescriptions = new Dictionary<PropertyID, string[]>();
            PropertyDescriptions[PropertyID.SPECTRUM_RBW] = new[] { "Res. Bandwidth", "MHz" };
            PropertyDescriptions[PropertyID.SPECTRUM_VBW] = new[] { "Video Bandwidth", "MHz" };
        }

        public enum DataType { SPECTRUM = 5, TIME_TRACE = 6, S_PARAMETER = 10, SCREEN_CAPTURE = 20 }

        public enum SeriesType { DATA = 0, MASK, MEMORY }

        public enum PropertyID
        {
            GENERAL1 = 1,
            GENERAL2 = 2,
            GENERAL3 = 3,

            _PREDEFINED = 0x10000,
            SPECTRUM_RBW = 0x10000 + 1,
            SPECTRUM_VBW = 0x10000 + 2,
        }

        [Serializable]
        public sealed class SeriesPoints
        {
            private string m_name;
            private double[] m_Xs;
            private double[] m_Y1s;
            private double[] m_Y2s;
            private SeriesType m_type;

            public SeriesPoints(string name, IEnumerable<double> xs, IEnumerable<double> y1s) : this(name, xs, y1s, null)
            {
            }

            public SeriesPoints(string name, IEnumerable<double> xs, IEnumerable<double> y1s, IEnumerable<double> y2s) : this(name, xs, y1s, y2s, SeriesType.DATA)
            {
            }

            public SeriesPoints(string name, IEnumerable<double> xs, IEnumerable<double> y1s, IEnumerable<double> y2s, SeriesType type)
            {
                m_name = name;
                m_type = type;
                m_Y1s = y1s.ToArray();
                m_Xs = (xs == null ? null : xs.ToArray());
                m_Y2s = (y2s == null ? null : y2s.ToArray());
            }

            public string Name { get { return m_name; } }
            public int Count { get { return m_Y1s.Length; } }
            public double[] Xs { get { return m_Xs; } }
            public double[] Y1s { get { return m_Y1s; } }
            public double[] Y2s { get { return m_Y2s; } }

            public bool IsXNull()
            {
                return m_Xs == null;
            }

            public bool IsY2Null()
            {
                return m_Y2s == null;
            }

            public double GetXAt(int index)
            {
                return (m_Xs == null ? index : m_Xs[index]);
            }

            public double GetY1At(int index)
            {
                return m_Y1s[index];
            }

            public double GetY2At(int index)
            {
                return m_Y2s[index];
            }

            public double[] GetXsClone()
            {
                return (double[])m_Xs.Clone();
            }

            public double[] GetY1sClone()
            {
                return (double[])m_Y1s.Clone();
            }

            public double[] GetY2sClone()
            {
                return (double[])m_Y2s.Clone();
            }
        }

        private DataType m_dataType;
        private List<SeriesPoints> m_seriesList;
        private Dictionary<PropertyID, object> m_properties;

        public SeriesData(DataType dataType)
        {
            m_dataType = dataType;
            m_seriesList = new List<SeriesPoints>();
            m_properties = new Dictionary<PropertyID, object>();
        }

        public DataType Type { get { return m_dataType; } }
        public int Count { get { return m_seriesList.Count; } }

        public void Add(SeriesPoints series)
        {
            m_seriesList.Add(series);
        }

        public SeriesPoints this[int index] { get { return m_seriesList[index]; } }
        public Dictionary<PropertyID, object> Properties { get { return m_properties; } }

        public void SetProperty(PropertyID propId, object value)
        {
            m_properties[propId] = value;
        }

        public object GetProperty(PropertyID propId)
        {
            object value;
            return m_properties.TryGetValue(propId, out value) ? value : null;
        }

        public string GetPropertyDescription(PropertyID propId)
        {
            object value;
            if (m_properties.TryGetValue(propId, out value))
            {
                if (propId > PropertyID._PREDEFINED)
                {
                    string[] descriptions;
                    if (PropertyDescriptions.TryGetValue(propId, out descriptions))
                        return $"√ {descriptions[0]}: {value}{descriptions[1]}";
                    else
                        return $"√ {propId}: {value}";
                }
                else
                    return $"√ {value}";
            }
            return null;
        }

        public IEnumerator<SeriesData.SeriesPoints> GetEnumerator()
        {
            return m_seriesList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_seriesList.GetEnumerator();
        }
    }
}