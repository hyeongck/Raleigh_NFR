using System;

namespace ClothoSharedItems.Common
{
    [Serializable]
    public struct WCValue
    {
        private int m_cnt;
        private double m_sum;
        private double m_min, m_max;//m_median

        public bool IsValid { get { return m_cnt > 0; } }
        public double Count { get { return m_cnt; } }
        public double Sum { get { return (m_cnt > 0 ? m_sum : double.NaN); } }
        public double Mean { get { return (m_cnt > 0 ? m_sum / m_cnt : double.NaN); } }
        public double Maximum { get { return (m_cnt > 0 ? m_max : double.NaN); } }
        public double Minimum { get { return (m_cnt > 0 ? m_min : double.NaN); } }
        public double Range { get { return (m_cnt > 0 ? m_max - m_min : double.NaN); } }
        public double Median { get { return 0; } }

        public void Clear()
        {
            m_cnt = 0; m_sum = m_max = m_min = 0;
        }

        public void Add(double value)
        {
            m_cnt++;
            m_sum += value;
            if (m_cnt <= 1)
            {
                m_max = m_min = value;
            }
            else
            {
                if (value > m_max) m_max = value;
                if (value < m_min) m_min = value;
            }
        }

        public void Set(int count, double mean, double min, double max)
        {
            m_cnt = count; m_sum = count * mean; m_min = min; m_max = max;
        }

        public static double GetMedian(double[] sourceNumbers)
        {
            //Framework 2.0 version of this method. there is an easier way in F4
            if (sourceNumbers == null || sourceNumbers.Length == 0)
                throw new System.Exception("Median of empty array not defined.");

            //make sure the list is sorted, but use a new array
            double[] sortedPNumbers = (double[])sourceNumbers.Clone();
            Array.Sort(sortedPNumbers);

            //get the median
            int size = sortedPNumbers.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedPNumbers[mid] : ((double)sortedPNumbers[mid] + (double)sortedPNumbers[mid - 1]) / 2;
            return median;
        }
    }
}