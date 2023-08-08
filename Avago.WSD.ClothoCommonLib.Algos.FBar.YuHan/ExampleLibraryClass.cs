using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading; 

namespace Avago.WSD.ClothoCommonLib.Algos.FBar.YuHan
{
    /// <summary>
    /// Dummy class with dummy algo method to demo how a library should looks like 
    /// </summary>
    public class ExampleLibraryClass
    {
        public static double ExampleMaxAlgo(double x, double y)
        {
            return (x > y) ? x : y; 
        }

        public static double ExampleMinAlgo(double x, double y)
        {
            return (x > y) ? y : x;
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }


    public static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

}



