using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ClothoSharedItems
{
    public static class ArrayAddin
    {
        public static List<FileInfo> GetLastFileModifiedFast3(string path, string searchPatternExpression = "", SearchOption searchOption = SearchOption.AllDirectories)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);
            List<string> retVal = new List<string>();

            DateTime retval = DateTime.MinValue;
            var dInfo = new DirectoryInfo(path);
            IEnumerable<FileInfo> files = null;

            try
            {
                files = dInfo.EnumerateFiles() //(searchPatternExpression, SearchOption.TopDirectoryOnly)
                                    .Where(file => reSearchPattern.IsMatch(file.Extension))
                                    .Union
                                    (
                                        dInfo.EnumerateDirectories()
                                                        .AsParallel()
                                                        .SelectMany(di => di.EnumerateFiles("*", SearchOption.AllDirectories)
                                                                            .Where(file => reSearchPattern.IsMatch(file.Extension))
                                    ));
            }
            catch { }

            var tt = files?.ToList();
            return tt;
        }

        public static List<string> GetFiles(string path,
                       string searchPatternExpression, bool matchfilename = false)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);
            List<string> retVal = new List<string>();
            try
            {
                retVal = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                                  .Where(file => (matchfilename) ? reSearchPattern.IsMatch(Path.GetFileName(file)) : reSearchPattern.IsMatch(Path.GetExtension(file)))
                                  .ToList();
                foreach (DirectoryInfo d in new DirectoryInfo(path).GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    retVal.AddRange(GetFiles(d.FullName, searchPatternExpression, matchfilename));
                }
            }
            catch (Exception)
            {
                //Console.WriteLine(dirPath);
            }
            return retVal;
        }

        public static IEnumerable<string> GetFiles(string path,
                       string[] searchPatterns,
                       SearchOption searchOption = SearchOption.AllDirectories)
        {
            return searchPatterns.AsParallel()
                   .SelectMany(searchPattern =>
                          Directory.EnumerateFiles(path, searchPattern, searchOption));
        }

        public static T[] Concact<T>(T[] arrayA, T[] arrayB)
        {
            if (arrayA == null)
            {
                if (arrayB == null)
                {
                    return null;
                }
                else
                {
                    return (T[])(arrayB.Clone());
                }
            }
            else
            {
                if (arrayB == null)
                {
                    return (T[])(arrayA.Clone());
                }
                else
                {
                    T[] arrayT = new T[arrayA.Length + arrayB.Length];
                    arrayA.CopyTo(arrayT, 0);
                    arrayB.CopyTo(arrayT, arrayA.Length);
                    return arrayT;
                }
            }
        }

        public static T[] Concact<T>(T[] arrayA, T itemB)
        {
            if (arrayA == null)
            {
                if (itemB == null)
                {
                    return null;
                }
                else
                {
                    return new T[] { itemB };
                }
            }
            else
            {
                if (itemB == null)
                {
                    return (T[])(arrayA.Clone());
                }
                else
                {
                    T[] arrayT = new T[arrayA.Length + 1];
                    arrayA.CopyTo(arrayT, 0);
                    arrayT[arrayA.Length] = itemB;
                    return arrayT;
                }
            }
        }

        public static string JoinToString(this IEnumerable<string> values, string separator)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string value in values)
            {
                if (sb.Length > 0) sb.Append(separator);
                sb.Append(value);
            }
            return sb.ToString();
        }

        public static string JoinToString<T>(this IEnumerable<T> values, string separator)
        {
            StringBuilder sb = new StringBuilder();
            foreach (T val in values)
            {
                if (sb.Length > 0) sb.Append(separator);
                sb.Append(val.ToString());
            }
            return sb.ToString();
        }

        public static string JoinToString<T>(this IEnumerable<T> values, string separator, Converter<T, string> converter)
        {
            StringBuilder sb = new StringBuilder();
            foreach (T val in values)
            {
                if (sb.Length > 0) sb.Append(separator);
                sb.Append(converter(val));
            }
            return sb.ToString();
        }

        public static bool TrueForAll<T>(this T[] array, Predicate<T> match)
        {
            return Array.TrueForAll(array, match);
        }

        public static bool Contains<T>(this T[] array, T value)
        {
            return Array.IndexOf(array, value) >= 0;
        }
    }

    public static class ForArray
    {
        public static void Set<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = value;
        }

        public static void Set<T>(this T[] array, int start, int length, T value)
        {
            for (int i = start; i < start + length; i++)
                array[i] = value;
        }

        public static int IndexOf<T>(this T[] array, Predicate<T> predicate)
        {
            if (array == null) return -1;

            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i])) return i;
            }
            return -1;
        }

        public static int LastIndexOf<T>(this T[] array, Predicate<T> predicate)
        {
            if (array == null) return -1;
            for (int i = array.Length - 1; i >= 0; i--)
            {
                if (predicate(array[i])) return i;
            }
            return -1;
        }

        public static bool Exists<T>(this T[] array, Predicate<T> match)
        {
            return Array.Exists(array, match);
        }

        public static bool Exists<T>(this T[] array, Func<T, int, bool> match)
        {
            for (int i = 0; i < array.Length; i++)
                if (match(array[i], i)) return true;
            return false;
        }

        public static bool TrueForAll<T>(this T[] array, Func<T, int, bool> match)
        {
            for (int i = 0; i < array.Length; i++)
                if (!match(array[i], i)) return false;
            return true;
        }

        public static void ForEach<T>(this T[] array, Action<T> action)
        {
            Array.ForEach(array, action);
        }

        public static void ForEach<T>(this T[] array, Action<T, int> action)
        {
            for (int i = 0; i < array.Length; i++)
                action(array[i], i);
        }

        public static T[] SubArray<T>(this T[] array, int index)
        {
            T[] newArray = new T[array.Length - index];
            Array.Copy(array, index, newArray, 0, newArray.Length);
            return newArray;
        }

        public static T[] SubArray<T>(this T[] array, int index, int count)
        {
            T[] newArray = new T[count];
            Array.Copy(array, index, newArray, 0, count);
            return newArray;
        }

        public static T[] AddArray<T>(this T[] arrayA, T[] arrayB)
        {
            if (arrayA == null)
            {
                return (arrayB == null ? null : (T[])arrayB.Clone());
            }
            else
            {
                if (arrayB == null)
                    return (T[])(arrayA.Clone());
                else
                {
                    T[] arrayT = new T[arrayA.Length + arrayB.Length];
                    arrayA.CopyTo(arrayT, 0);
                    arrayB.CopyTo(arrayT, arrayA.Length);
                    return arrayT;
                }
            }
        }

        public static T[] Samples<T>(this T[] array, int interval, int startIndex)
        {
            T[] sampledArray = new T[(array.Length - startIndex - 1) / interval + 1];
            for (int i = 0; i < sampledArray.Length; i++)
                sampledArray[i] = array[startIndex + interval * i];
            return sampledArray;
        }

        public static T[][] GroupByN<T>(this T[] array, int num)
        {
            T[][] groupArray = new T[array.Length / num][];
            for (int i = 0; i < groupArray.Length; i++)
                groupArray[i] = array.SubArray(i * num, num);
            return groupArray;
        }

        public static string[] Uniquenize(this string[] values)
        {
            if (values == null)
                return null;
            else
            {
                var uqValues = new String[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    var uqValue = values[i] ?? string.Empty;
                    for (int s = 1; s < int.MaxValue; s++)
                    {
                        int j = 0;
                        for (; j < i; j++)
                        {
                            if (uqValue == uqValues[j]) break;
                        }
                        if (j >= i) break;
                        uqValue = (values[i] + "_" + s);
                    }
                    uqValues[i] = uqValue;
                }
                return uqValues;
            }
        }

        public static string Uniquenize(this IEnumerable<string> values, string value)
        {
            if (values.Any(v => v == value))
            {
                int i = 1;
                do
                {
                    var uqvalue = value + "_" + i;
                    if (values.All(v => v != uqvalue))
                        return uqvalue;
                    i++;
                } while (true);
            }
            return value;
        }

        public static bool HasSame<T>(this IEnumerable<T> values, Func<T, object> selector)
        {
            object firstOne = null;
            foreach (T value in values)
            {
                if (firstOne == null)
                    firstOne = selector(value);
                else
                {
                    if (!firstOne.Equals(selector(value))) return false;
                }
            }
            return true;
        }

        public static IDictionary<TKey, TValue> ToDictionaryEx<TElement, TKey, TValue>(
           this IEnumerable<TElement> source,
           Func<TElement, TKey> keyGetter,
           Func<TElement, TValue> valueGetter)
        {
            IDictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            foreach (var e in source)
            {
                var key = keyGetter(e);
                if (dict.ContainsKey(key))
                {
                    continue;
                }

                dict.Add(key, valueGetter(e));
            }
            return dict;
        }

        public static string GetValue(this IEnumerable<ATFConfigXmlField> type, string name)
        {
            return type.FirstOrDefault(t => t.name.Equals(name, StringComparison.OrdinalIgnoreCase))?.value;
        }

        public static T MergeLeft<T, K, V>(this T me, params IDictionary<K, V>[] others) where T : IDictionary<K, V>, new()
        {
            T newMap = new T();
            foreach (IDictionary<K, V> src in
                (new List<IDictionary<K, V>> { me }).Concat(others))
            {
                foreach (KeyValuePair<K, V> p in src)
                {
                    newMap[p.Key] = p.Value;
                }
            }
            return newMap;
        }

        public static void RenameKey<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey fromKey, TKey toKey)
        {
            TValue value = dic[fromKey];
            dic.Remove(fromKey);
            dic[toKey] = value;
        }
    }
}