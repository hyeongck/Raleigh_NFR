using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClothoSharedItems
{
    public static class ForString
    {
        private static string HexNumHeader = "0x";
        private static readonly char[] SeparatorChars = { ',', ' ', '~', (char)9 };
        private static readonly char[] HexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f' };

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool IsSeriesOfDecDigits(this string value)
        {
            return value.ToCharArray().TrueForAll(c => char.IsDigit(c));
        }

        public static bool IsSeriesOfHexDigits(this string value)
        {
            return value.ToCharArray().TrueForAll(c => HexDigits.Contains(c));
        }

        #region case-insensitive comparison function

        public static bool CIvEquals(this string value, string what)
        {
            return string.Equals(value, what, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CIvEqualsAnyOf(this string value, params string[] whats)
        {
            foreach (string what in whats)
                if (value.CIvEquals(what)) return true;
            return false;
        }

        public static bool CIvEqualsAnyOf(this char value, params string[] whats)
        {
            foreach (string what in whats)
                if (value.ToString().CIvEquals(what)) return true;
            return false;
        }

        public static bool CIvStartsWith(this string value, string what)
        {
            if (value == null) return false;
            return value.StartsWith(what, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CIvStartsWithAnyOf(this string value, params string[] whats)
        {
            foreach (string what in whats)
                if (value.CIvStartsWith(what)) return true;
            return false;
        }

        public static bool CIvEndsWith(this string value, string what)
        {
            if (value == null) return false;
            return value.EndsWith(what, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CIvEndsWithAnyOf(this string value, params string[] whats)
        {
            foreach (string what in whats)
                if (value.CIvEndsWith(what)) return true;
            return false;
        }

        public static bool CIvContains(this string value, string what)
        {
            if (value == null) return false;
            return (value.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool CIvContainsAnyOf(this string value, params string[] whats)
        {
            foreach (string what in whats)
                if (value.CIvContains(what)) return true;
            return false;
        }

        public static bool CIvContainsAllOf(this string value, params string[] whats)
        {
            foreach (string what in whats)
                if (!value.CIvContains(what)) return false;
            return true;
        }

        #endregion case-insensitive comparison function

        #region split function : SubStringFirst, SubstringLast, SplitToArray, TokenFirst, TokenLast

        public static string SubStringFirst(this string value)
        {
            var nodes = value.SplitToArray();
            return nodes.Length > 0 ? nodes[0] : null;
        }

        public static string SubStringFirst(this string value, params char[] chars)
        {
            var nodes = value.SplitToArray(chars);
            return nodes.Length > 0 ? nodes[0] : null;
        }

        public static string SubstringLast(this string value, int length)
        {
            return value.Substring(value.Length - length);
        }

        public static string SubstringLastChar(this string value, params char[] chars)
        {
            var isinIndex = -1;
            foreach (var c_ in chars)
            {
                isinIndex = value.LastIndexOf(c_);
                if (isinIndex > 0) break;
            }
            if (isinIndex > 0) return value.Substring(0, isinIndex);
            else return value;
        }

        public static string[] SplitToArray(this string value)
        {
            return value.Split(SeparatorChars, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] SplitToArray(this string value, params char[] chars)
        {
            return value.Split(chars, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string TokenFirst(this string value)
        {
            return value.TokenFirst(SeparatorChars);
        }

        public static string TokenFirst(this string value, params char[] chars)
        {
            if (value == null) return null;
            int iPos = value.IndexOfAny(chars);
            return (iPos < 0 ? value : value.Substring(0, iPos));
        }

        public static string TokenFirst(this string value, out string remain)
        {
            return value.TokenFirst(out remain, SeparatorChars);
        }

        public static string TokenFirst(this string value, out string remain, params char[] chars)
        {
            if (value == null) { remain = null; return null; }
            int iPos = value.IndexOfAny(chars);
            remain = (iPos < 0 ? string.Empty : value.Substring(iPos + 1));
            return (iPos < 0 ? value : value.Substring(0, iPos));
        }

        public static string TokenLast(this string value)
        {
            return value.TokenLast(SeparatorChars);
        }

        public static string TokenLast(this string value, params char[] chars)
        {
            if (value == null) return null;
            int iPos = value.LastIndexOfAny(chars);
            return (iPos < 0 ? value : value.Substring(iPos + 1));
        }

        public static string TokenLast(this string value, out string remain)
        {
            return value.TokenLast(out remain, SeparatorChars);
        }

        public static string TokenLast(this string value, out string remain, params char[] chars)
        {
            if (value == null) { remain = null; return null; }
            int iPos = value.LastIndexOfAny(chars);
            remain = (iPos < 0 ? string.Empty : value.Substring(0, iPos));
            return (iPos < 0 ? value : value.Substring(iPos + 1));
        }

        public static string SubStringBetween(this string value, string whatFirst, string whatLast, bool removeDefaultSeparators)
        {
            var indexFirst = value.IndexOf(whatFirst);
            if (indexFirst < 0) return null;
            indexFirst += whatFirst.Length;

            var indexLast = value.IndexOf(whatLast, indexFirst + 1);
            if (indexLast < 0) return null;

            if (removeDefaultSeparators)
            {
                while (indexFirst < indexLast)
                {
                    if (Array.TrueForAll(SeparatorChars, v => v != value[indexFirst]))
                        break;
                    indexFirst++;
                }

                while (indexFirst < indexLast)
                {
                    if (Array.TrueForAll(SeparatorChars, v => v != value[indexLast - 1]))
                        break;
                    indexLast--;
                }
            }
            return value.Substring(indexFirst, indexLast - indexFirst);
        }

        #endregion split function : SubStringFirst, SubstringLast, SplitToArray, TokenFirst, TokenLast

        #region conversion function : ToByte, TryToByte, ToByteArray, TryToByteArray

        public static Byte ToByte(this string value, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return Byte.Parse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer);
        }

        public static Byte ToByteOrDefault(this string value, Byte defaultValue, bool forceHex = false)
        {
            Byte result; return value.TryToByte(out result, forceHex) ? result : defaultValue;
        }

        public static Byte TryToByte(this string value, Byte defaultvalue, bool forceHex = false)
        {
            Byte result; return value.TryToByte(out result, forceHex) ? result : defaultvalue;
        }

        public static bool TryToByte(this string value, bool forceHex = false)
        {
            Byte result; return value.TryToByte(out result, forceHex);
        }

        public static bool TryToByte(this string value, Byte min, Byte max, bool forceHex = false)
        {
            Byte result; return value.TryToByte(min, max, out result, forceHex);
        }

        public static bool TryToByte(this string value, Byte min, Byte max, out Byte result, bool forceHex = false)
        {
            return (value.TryToByte(out result, forceHex) && min <= result && result <= max);
        }

        public static bool TryToByte(this string value, out Byte result, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return Byte.TryParse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer, null, out result);
        }

        public static Byte[] ToByteArray(this string value, bool forceHex = false)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToByte(forceHex));
        }

        public static bool TryToByteArray(this string value, out byte[] result, bool forceHex = false)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            byte[] byteVals = new byte[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToByte(out byteVals[i], forceHex)) return false;
            }
            result = byteVals;
            return true;
        }

        public static Byte[] NoneSplitableToByteArray(this string HexString)
        {
            int NumberChars = HexString.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
            }
            return bytes;
        }

        #endregion conversion function : ToByte, TryToByte, ToByteArray, TryToByteArray

        #region conversion function : ToSByte, TryToSByte, ToSByteArray, TryToSByteArray

        public static SByte ToSByte(this string value, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return SByte.Parse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer);
        }

        public static SByte TryToSByte(this string value, SByte defaultvalue, bool forceHex = false)
        {
            SByte result; return value.TryToSByte(out result, forceHex) ? result : defaultvalue;
        }

        public static bool TryToSByte(this string value, bool forceHex = false)
        {
            SByte result; return value.TryToSByte(out result, forceHex);
        }

        public static bool TryToSByte(this string value, SByte min, SByte max, bool forceHex = false)
        {
            SByte result; return value.TryToSByte(min, max, out result, forceHex);
        }

        public static bool TryToSByte(this string value, SByte min, SByte max, out SByte result, bool forceHex = false)
        {
            return (value.TryToSByte(out result, forceHex) && min <= result && result <= max);
        }

        public static bool TryToSByte(this string value, out SByte result, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return SByte.TryParse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer, null, out result);
        }

        public static SByte[] ToSByteArray(this string value, bool forceHex = false)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToSByte(forceHex));
        }

        public static bool TryToSByteArray(this string value, out SByte[] result, bool forceHex = false)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            SByte[] byteVals = new SByte[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToSByte(out byteVals[i], forceHex)) return false;
            }
            result = byteVals;
            return true;
        }

        #endregion conversion function : ToSByte, TryToSByte, ToSByteArray, TryToSByteArray

        #region conversion function : ToInt16, TryToInt16, ToInt16Array, TryToInt16Array

        public static Int16 ToInt16(this string value, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return Int16.Parse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer);
        }

        public static bool TryToInt16(this string value, out Int16 result, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return Int16.TryParse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer, null, out result);
        }

        public static Int16 ToInt16(this string value, Int16 defaultvalue, bool forceHex = false)
        {
            Int16 result; return value.TryToInt16(out result, forceHex) ? result : defaultvalue;
        }

        public static bool TryToInt16(this string value, bool forceHex = false)
        {
            Int16 result; return value.TryToInt16(out result, forceHex);
        }

        public static bool TryToInt16(this string value, Int16 min, Int16 max, out Int16 result, bool forceHex = false)
        {
            return (value.TryToInt16(out result, forceHex) && (min <= result && result <= max));
        }

        public static bool TryToInt16(this string value, Int16 min, Int16 max, bool forceHex = false)
        {
            Int16 result; return value.TryToInt16(min, max, out result, forceHex);
        }

        public static Int16[] ToInt16Array(this string value, bool forceHex = false)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToInt16(forceHex));
        }

        public static bool TryToInt16Array(this string value, out Int16[] result, bool forceHex = false)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            Int16[] intVals = new Int16[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToInt16(out intVals[i], forceHex)) return false;
            }
            result = intVals;
            return true;
        }

        #endregion conversion function : ToInt16, TryToInt16, ToInt16Array, TryToInt16Array

        #region conversion function : ToUInt16, TryToUInt16, ToUInt16Array, TryToUInt16Array

        public static UInt16 ToUInt16(this string value, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return UInt16.Parse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer);
        }

        public static bool TryToUInt16(this string value, out UInt16 result, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return UInt16.TryParse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer, null, out result);
        }

        public static UInt16 ToUInt16(this string value, UInt16 defaultvalue, bool forceHex = false)
        {
            UInt16 result; return value.TryToUInt16(out result, forceHex) ? result : defaultvalue;
        }

        public static bool TryToUInt16(this string value, bool forceHex = false)
        {
            UInt16 result; return value.TryToUInt16(out result, forceHex);
        }

        public static bool TryToUInt16(this string value, UInt16 min, UInt16 max, out UInt16 result, bool forceHex = false)
        {
            return (value.TryToUInt16(out result, forceHex) && (min <= result && result <= max));
        }

        public static bool TryToUInt16(this string value, UInt16 min, UInt16 max, bool forceHex = false)
        {
            UInt16 result; return value.TryToUInt16(min, max, out result, forceHex);
        }

        public static UInt16[] ToUInt16Array(this string value, bool forceHex = false)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToUInt16(forceHex));
        }

        public static bool TryToUInt16Array(this string value, out UInt16[] result, bool forceHex = false)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            UInt16[] intVals = new UInt16[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToUInt16(out intVals[i], forceHex)) return false;
            }
            result = intVals;
            return true;
        }

        #endregion conversion function : ToUInt16, TryToUInt16, ToUInt16Array, TryToUInt16Array

        #region conversion function : ToInt32, TryToInt32, ToInt32Array, TryToInt32Array

        public static Int32 ToInt32(this string value, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return Int32.Parse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer);
        }

        public static Int32 ToInt32OrDefault(this string value)
        {
            Int32 result; return value.TryToInt32(out result) ? result : 0;
        }

        public static Int32 ToInt32OrDefault(this string value, Int32 defaultvalue, bool forceHex = false)
        {
            Int32 result; return value.TryToInt32(out result, forceHex) ? result : defaultvalue;
        }

        public static bool TryToInt32(this string value, out Int32 result, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return Int32.TryParse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer, null, out result);
        }

        public static bool TryToInt32InRange(this string value, Int32 min, Int32 max, out Int32 result, bool forceHex = false)
        {
            return (value.TryToInt32(out result, forceHex) && (min <= result && result <= max));
        }

        public static bool ParsableToInt32(this string value, bool forceHex = false)
        {
            Int32 result; return value.TryToInt32(out result, forceHex);
        }

        public static bool ParsableToInt32InRange(this string value, Int32 min, Int32 max, bool forceHex = false)
        {
            Int32 result; return value.TryToInt32InRange(min, max, out result, forceHex);
        }

        public static Int32[] ToInt32Array(this string value, bool forceHex = false)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToInt32(forceHex));
        }

        public static bool TryToInt32Array(this string value, out Int32[] result, bool forceHex = false)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            Int32[] intVals = new Int32[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToInt32(out intVals[i], forceHex)) return false;
            }
            result = intVals;
            return true;
        }

        #endregion conversion function : ToInt32, TryToInt32, ToInt32Array, TryToInt32Array

        #region conversion function : ToUInt32, TryToUInt32, ToUInt32Array, TryToUInt32Array

        public static UInt32 ToUInt32(this string value, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return UInt32.Parse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer);
        }

        public static bool TryToUInt32(this string value, out UInt32 result, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return UInt32.TryParse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer, null, out result);
        }

        public static UInt32 ToUInt32(this string value, UInt32 defaultvalue, bool forceHex = false)
        {
            UInt32 result; return value.TryToUInt32(out result, forceHex) ? result : defaultvalue;
        }

        public static bool TryToUInt32(this string value, bool forceHex = false)
        {
            UInt32 result; return value.TryToUInt32(out result, forceHex);
        }

        public static bool TryToUInt32(this string value, UInt32 min, UInt32 max, out UInt32 result, bool forceHex = false)
        {
            return (value.TryToUInt32(out result, forceHex) && (min <= result && result <= max));
        }

        public static bool TryToUInt32(this string value, UInt32 min, UInt32 max, bool forceHex = false)
        {
            UInt32 result; return value.TryToUInt32(min, max, out result, forceHex);
        }

        public static UInt32[] ToUInt32Array(this string value, bool forceHex = false)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToUInt32(forceHex));
        }

        public static bool TryToUInt32Array(this string value, out UInt32[] result, bool forceHex = false)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            UInt32[] intVals = new UInt32[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToUInt32(out intVals[i], forceHex)) return false;
            }
            result = intVals;
            return true;
        }

        #endregion conversion function : ToUInt32, TryToUInt32, ToUInt32Array, TryToUInt32Array

        #region conversion function : ToInt64, TryToInt64, ToInt64Array, TryToInt64Array

        public static Int64 ToInt64(this string value, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return Int64.Parse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer);
        }

        public static bool TryToInt64(this string value, out Int64 result, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return Int64.TryParse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer, null, out result);
        }

        public static Int64 ToInt64(this string value, Int64 defaultvalue, bool forceHex = false)
        {
            Int64 result; return value.TryToInt64(out result, forceHex) ? result : defaultvalue;
        }

        public static bool TryToInt64(this string value, bool forceHex = false)
        {
            Int64 result; return value.TryToInt64(out result, forceHex);
        }

        public static bool TryToInt64(this string value, Int64 min, Int64 max, out Int64 result, bool forceHex = false)
        {
            return (value.TryToInt64(out result, forceHex) && (min <= result && result <= max));
        }

        public static bool TryToInt64(this string value, Int64 min, Int64 max, bool forceHex = false)
        {
            Int64 result; return value.TryToInt64(min, max, out result, forceHex);
        }

        public static Int64[] ToInt64Array(this string value, bool forceHex = false)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToInt64(forceHex));
        }

        public static bool TryToInt64Array(this string value, out Int64[] result, bool forceHex = false)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            Int64[] intVals = new Int64[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToInt64(out intVals[i], forceHex)) return false;
            }
            result = intVals;
            return true;
        }

        #endregion conversion function : ToInt64, TryToInt64, ToInt64Array, TryToInt64Array

        #region conversion function : ToUInt64, TryToUInt64, ToUInt64Array, TryToUInt64Array

        public static UInt64 ToUInt64(this string value, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return UInt64.Parse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer);
        }

        public static bool TryToUInt64(this string value, out UInt64 result, bool forceHex = false)
        {
            if (value.CIvStartsWith(HexNumHeader)) { forceHex = true; value = value.Substring(2); }
            return UInt64.TryParse(value, forceHex ? NumberStyles.HexNumber : NumberStyles.Integer, null, out result);
        }

        public static UInt64 ToUInt64(this string value, UInt64 defaultvalue, bool forceHex = false)
        {
            UInt64 result; return value.TryToUInt64(out result, forceHex) ? result : defaultvalue;
        }

        public static bool TryToUInt64(this string value, bool forceHex = false)
        {
            UInt64 result; return value.TryToUInt64(out result, forceHex);
        }

        public static bool TryToUInt64(this string value, UInt64 min, UInt64 max, out UInt64 result, bool forceHex = false)
        {
            return (value.TryToUInt64(out result, forceHex) && (min <= result && result <= max));
        }

        public static bool TryToUInt64(this string value, UInt64 min, UInt64 max, bool forceHex = false)
        {
            UInt64 result; return value.TryToUInt64(min, max, out result, forceHex);
        }

        public static UInt64[] ToUInt64Array(this string value, bool forceHex = false)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToUInt64(forceHex));
        }

        public static bool TryToUInt64Array(this string value, out UInt64[] result, bool forceHex = false)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            UInt64[] intVals = new UInt64[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToUInt64(out intVals[i], forceHex)) return false;
            }
            result = intVals;
            return true;
        }

        #endregion conversion function : ToUInt64, TryToUInt64, ToUInt64Array, TryToUInt64Array

        #region conversion function : ToDouble, TryToDouble, ToDoubleArray, TryToDoubleArray

        public static Double ToDouble(this string value)
        {
            return Double.Parse(value);
        }

        public static Double ToDoubleOrDefault(this string value)
        {
            Double result; return value.TryToDouble(out result) ? result : double.NaN;
        }

        public static Double ToDoubleOrDefault(this string value, double defaultvalue)
        {
            Double result; return value.TryToDouble(out result) ? result : defaultvalue;
        }

        public static bool TryToDouble(this string value, out Double result)
        {
            return Double.TryParse(value, out result);
        }

        public static bool TryToDouble(this string value)
        {
            Double result; return Double.TryParse(value, out result);
        }

        public static bool TryToDouble(this string value, Double min, Double max)
        {
            Double result; return value.TryToDouble(min, max, out result);
        }

        public static bool TryToDouble(this string value, Double min, Double max, out Double result)
        {
            return (value.TryToDouble(out result) && min <= result && result <= max);
        }

        public static Double[] ToDoubleArray(this string value)
        {
            string[] strVals = value.SplitToArray();
            return Array.ConvertAll(strVals, x => x.ToDouble());
        }

        public static bool TryToDoubleArray(this string value, out Double[] result)
        {
            result = null;
            string[] strVals = value.SplitToArray();
            Double[] dblVals = new Double[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToDouble(out dblVals[i])) return false;
            }
            result = dblVals;
            return true;
        }

        public static bool TryToDoubleArray(this string value, out Double[] result, params char[] chars)
        {
            result = null;
            string[] strVals = value.SplitToArray(chars);
            Double[] dblVals = new Double[strVals.Length];
            for (int i = 0; i < strVals.Length; i++)
            {
                if (!strVals[i].TryToDouble(out dblVals[i])) return false;
            }
            result = dblVals;
            return true;
        }

        #endregion conversion function : ToDouble, TryToDouble, ToDoubleArray, TryToDoubleArray

        #region conversion function : ToBoolean, TryToBoolean

        public static bool ToBoolean(this string value)
        {
            return Boolean.Parse(value);
        }

        public static bool ToBooleanOrDefault(this string value, bool defaultValue)
        {
            bool result; return value.TryToBoolean(out result) ? result : defaultValue;
        }

        public static bool TryToBoolean(this string value, out bool result)
        {
            return Boolean.TryParse(value, out result);
        }

        #endregion conversion function : ToBoolean, TryToBoolean

        #region conversion function : GetInt

        public static int GetInt(this string value)
        {
            if (value != null)
            {
                string strTmp = System.Text.RegularExpressions.Regex.Replace(value, @"\D", "");
                var val = int.TryParse(strTmp, out int scel);
                return val ? scel : -1;
            }
            else
                return -1;
        }

        #endregion conversion function : GetInt

        #region enum-related function

        public static TEnum ToEnum<TEnum>(this string value) where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value, true);
        }

        public static TEnum ToEnumOrDefault<TEnum>(this string value) where TEnum : struct
        {
            TEnum result;
            return Enum.TryParse(value, true, out result) ? result : default(TEnum);
        }

        public static TEnum ToEnumOrDefault<TEnum>(this string value, TEnum defaultValue) where TEnum : struct
        {
            TEnum result;
            return Enum.TryParse(value, true, out result) ? result : defaultValue;
        }

        public static bool TryToEnum<TEnum>(this string value, out TEnum result) where TEnum : struct
        {
            return Enum.TryParse(value, true, out result);
        }

        public static int ToEnumInt32OrDefault(this string value, Type enumType, int defaultValue)
        {
            return (value.TryToEnumInt32(enumType, out int enumValue) ? enumValue : defaultValue);
        }

        public static bool TryToEnumInt32(this string value, Type enumType, out int enumValue)
        {
            enumValue = 0;
            if (value.IsNullOrEmpty())
            {
                return false;
            }
            else
            {
                foreach (var vut in Enum.GetValues(enumType))
                {
                    if (value.CIvEquals(vut.ToString()))
                    {
                        enumValue = (int)vut;
                        return true;
                    }
                }
                return false;
            }
        }

        public static TEnum ToEnum<TEnum>(this string value, bool ignoreCase = true) where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
        }

        public static bool TryToEnum<TEnum>(this string value, out TEnum result, bool ignoreCase = true) where TEnum : struct
        {
            result = default(TEnum);
            return Enum.TryParse<TEnum>(value, ignoreCase, out result);
        }

        #endregion enum-related function

        #region Reverse

        public static IEnumerable<string> StringCluster(this string s)
        {
            var enumerator = StringInfo.GetTextElementEnumerator(s);
            while (enumerator.MoveNext())
            {
                yield return (string)enumerator.Current;
            }
        }

        public static string ReverseStringCluster(this string s)
        {
            return string.Join("", s.StringCluster().Reverse().ToArray());
        }

        #endregion Reverse
    }
}