using System;
using System.Collections.Generic;

namespace ClothoSharedItems
{
    public static class ForNumeric
    {
        public static int Trim(this int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int TrimMin(this int value, int min)
        {
            return (value < min ? min : value);
        }

        public static int TrimMax(this int value, int max)
        {
            return (value > max ? max : value);
        }

        public static int QuantizeBy(this int value, int normValue)
        {
            return (int)Math.Round((double)value / normValue) * normValue;
        }

        public static int QuantizeUpBy(this int value, int normValue)
        {
            return (int)Math.Ceiling((double)value / normValue) * normValue;
        }

        public static int QuantizeDownBy(this int value, int normValue)
        {
            return (int)Math.Floor((double)value / normValue) * normValue;
        }

        public static bool IsInRange(this int value, int min, int max)
        {
            return (min <= value && value <= max);
        }

        public static bool IsInRangeAll(this int[] values, int min, int max)
        {
            return Array.TrueForAll(values, v => v.IsInRange(min, max));
        }

        public static bool IsInRangeAny(this int[] values, int min, int max)
        {
            return Array.Exists(values, v => v.IsInRange(min, max));
        }

        public static double Trim(this double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double TrimMin(this double value, double min)
        {
            return (value < min ? min : value);
        }

        public static double TrimMax(this double value, double max)
        {
            return (value > max ? max : value);
        }

        public static double QuantizeBy(this double value, double normValue)
        {
            return Math.Round(value / normValue) * normValue;
        }

        public static double QuantizeUpBy(this double value, double normValue)
        {
            return Math.Ceiling(value / normValue) * normValue;
        }

        public static double QuantizeDownBy(this double value, double normValue)
        {
            return Math.Floor(value / normValue) * normValue;
        }

        public static bool IsInRange(this double value, double min, double max)
        {
            return (min <= value && value <= max);
        }

        public static bool IsInRangeAll(this double[] values, double min, double max)
        {
            return Array.TrueForAll(values, v => v.IsInRange(min, max));
        }

        public static bool IsnRangeAny(this double[] values, double min, double max)
        {
            return Array.Exists(values, v => v.IsInRange(min, max));
        }

        /// <summary>
        /// To check string intVal is between start to end
        /// </summary>
        /// <param name="val">Value which want to check IsinRange</param>
        /// <param name="strStartNum">start value</param>
        /// <param name="strEndNum">end value</param>
        /// <returns></returns>
        public static bool IsInRange(this string val, string strStartNum, string strEndNum)
        {
            int.TryParse(val, System.Globalization.NumberStyles.HexNumber, null, out int intVal);
            int.TryParse(strStartNum, System.Globalization.NumberStyles.HexNumber, null, out int intStartNum);
            int.TryParse(strEndNum, System.Globalization.NumberStyles.HexNumber, null, out int intEndNum);

            if (intVal.IsInRange(intStartNum, intEndNum)) return true;
            else return false;
        }

        public static double RecommendedQuantizationSize(this double value, int digitsStart)
        {
            var iSubNum = 0;
            var iDigits = -digitsStart;
            var fRange = Math.Abs(value);
            var arySubNums = new[] { 1, 2, 5, 10 };
            //var arySubNums = new[] { 1, 2, 3, 4, 5, 6, 8, 10 };
            while (fRange >= Math.Pow(10, iDigits + 1)) iDigits++;
            for (iSubNum = 0; iSubNum < arySubNums.Length; iSubNum++)
            {
                if (arySubNums[iSubNum] * Math.Pow(10, iDigits) >= fRange) break;
            }
            return arySubNums[iSubNum] * Math.Pow(10, iDigits);
        }

        public static IEnumerable<int> EnumerateByStep(int start, int stop, int step)
        {
            for (int v = start; v <= stop; v += step)
                yield return v;
        }

        public static IEnumerable<double> EnumerateByCount(double start, double stop, int count)
        {
            for (int i = 0; i < count; i++)
                yield return (start * (count - i - 1) + stop * i) / (count - 1);
        }

        public static double PositionOf(this double[] values, double value)
        {
            if (value <= values[0])
                return 0;
            else
            {
                for (int i = 0; i < values.Length - 1; i++)
                {
                    if (value < values[i + 1])
                        return i + (value - values[i]) / (values[i + 1] - values[i]);
                }
                return values.Length - 1;
            }
        }

        public static double[] ArrayDouble(double start, double stop, int count)
        {
            double[] values = new double[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = (start * (count - 1 - i) + stop * i) / (count - 1);
            }
            return values;
        }
    }
}