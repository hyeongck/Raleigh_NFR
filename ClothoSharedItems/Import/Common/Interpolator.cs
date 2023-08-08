using System;
using System.Collections.Generic;
using System.Linq;

namespace ClothoSharedItems.Common
{
    public class Interpolator
    {
        private int m_cnt;
        private double[] m_xVals;
        private double[] m_yVals;

        public Interpolator(double[] xValues, double[] yValues)
        {
            if (xValues.Length != yValues.Length)
                throw new ArgumentException();
            m_cnt = xValues.Length; m_xVals = xValues; m_yVals = yValues;
        }

        public double XMin { get { return m_xVals[0]; } }
        public double XMax { get { return m_xVals[m_cnt - 1]; } }
        public double YMin { get { return m_yVals[0]; } }
        public double YMax { get { return m_yVals[m_cnt - 1]; } }
        public double XWidth { get { return m_xVals[m_cnt - 1] - m_xVals[0]; } }
        public double YWidth { get { return m_yVals[m_cnt - 1] - m_yVals[0]; } }

        public bool IsXRange(double xValue)
        {
            return (m_xVals[0] <= xValue && xValue < m_xVals[m_cnt - 1]);
        }

        public bool IsYRange(double yValue)
        {
            return (m_yVals[0] <= yValue && yValue < m_yVals[m_cnt - 1]);
        }

        public double[] XList { get { return m_xVals; } }
        public double[] YList { get { return m_yVals; } }

        public IEnumerable<KeyValuePair<double, double>> XYValues
        {
            get
            {
                for (int i = 0; i < m_xVals.Length; i++)
                {
                    yield return new KeyValuePair<double, double>(m_xVals[i], m_yVals[i]);
                }
            }
        }

        public double XToY(double xValue)
        {
            if (xValue <= m_xVals[0])
            {
                return m_yVals[0];
            }
            else
            {
                for (int i = 1; i < m_cnt; i++)
                {
                    if (xValue < m_xVals[i])
                    {
                        return ((m_xVals[i] - xValue) * m_yVals[i - 1] + (xValue - m_xVals[i - 1]) * m_yVals[i]) / (m_xVals[i] - m_xVals[i - 1]);
                    }
                }
                return m_yVals[m_cnt - 1];
            }
        }

        public bool Equals(Interpolator other)
        {
            if (other != null)
            {
                return m_xVals.SequenceEqual(other.m_xVals) && m_yVals.SequenceEqual(other.m_yVals);
            }
            return false;
        }
    }
}