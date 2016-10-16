using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ch09
{
    class Program
    {
        #region Fibonacci

        public static ulong FibonacciNumber(uint which)
        {
            if (which == 1 || which == 2) return 1;
            return FibonacciNumber(which - 2) + FibonacciNumber(which - 1);
        }

        public static ulong FibonacciNumberMemoization(uint which)
        {
            if (which == 1 || which == 2) return 1;
            ulong[] array = new ulong[which];
            array[0] = 1; array[1] = 1;
            return FibonacciNumberMemoization(which, array);
        }

        private static ulong FibonacciNumberMemoization(uint which, ulong[] array)
        {
            if (array[which - 3] == 0)
            {
                array[which - 3] = FibonacciNumberMemoization(which - 2, array);
            }
            if (array[which - 2] == 0)
            {
                array[which - 2] = FibonacciNumberMemoization(which - 1, array);
            }
            array[which - 1] = array[which - 3] + array[which - 2];
            return array[which - 1];
        }

        public static ulong FibonacciNumberIteration(ulong which)
        {
            if (which == 1 || which == 2) return 1;
            ulong a = 1, b = 1;
            for (ulong i = 2; i < which; ++i)
            {
                ulong c = a + b;
                a = b;
                b = c;
            }
            return b;
        }

        #endregion

        #region Edit Distance

        public static int EditDistance(string s, string t)
        {
            int m = s.Length, n = t.Length;
            int[,] ed = new int[m, n];
            for (int i = 0; i < m; ++i)
            {
                ed[i, 0] = i + 1;
            }
            for (int j = 0; j < n; ++j)
            {
                ed[0, j] = j + 1;
            }
            for (int j = 1; j < n; ++j)
            {
                for (int i = 1; i < m; ++i)
                {
                    if (s[i] == t[j])
                    {
                        //No operation required
                        ed[i, j] = ed[i - 1, j - 1];
                    }
                    else
                    {   //Minimum between deletion, insertion, and substitution
                        ed[i, j] = Math.Min(ed[i - 1, j] + 1, Math.Min(ed[i, j - 1] + 1, ed[i - 1, j - 1] + 1));
                    }
                }
            }
            for (int i = 0; i < m; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    Console.Write("{0} ", ed[i, j]);
                }
                Console.WriteLine();
            }
            return ed[m - 1, n - 1];
        }

        #endregion

        #region All-Pairs-Shortest-Paths (Floyd-Warshall)

        static short[,] costs;
        static short[,] next;

        public static void AllPairsShortestPaths(short[] vertices, bool[,] hasEdge)
        {
            int N = vertices.Length;
            costs = new short[N, N];
            next = new short[N, N];
            for (short i = 0; i < N; ++i)
            {
                for (short j = 0; j < N; ++j)
                {
                    costs[i, j] = hasEdge[i, j] ? (short)1 : short.MaxValue;
                    if (costs[i, j] == 1)
                        next[i, j] = -1; //Marker for direct edge
                }
            }
            for (short k = 0; k < N; ++k)
            {
                for (short i = 0; i < N; ++i)
                {
                    for (short j = 0; j < N; ++j)
                    {
                        if (costs[i, k] + costs[k, j] < costs[i, j])
                        {
                            costs[i, j] = (short)(costs[i, k] + costs[k, j]);
                            next[i, j] = k;
                        }
                    }
                }
            }
        }
        
        public string GetPath(short src, short dst)
        {
            if (costs[src, dst] == short.MaxValue) return "<no path>";
            short intermediate = next[src, dst];
            if (intermediate == -1) //Marker for direct edge
            {
                return "->"; //Direct path
            }
            return GetPath(src, intermediate) + intermediate + GetPath(intermediate, dst);
        }

        #endregion

        static void Main(string[] args)
        {
            EditDistance("stutter", "glutton");
            Console.ReadLine();

            for (uint i = 1; i < 10000; ++i)
            {
                Console.WriteLine("MemoizedFib({0}) = {1}", i, FibonacciNumberMemoization(i));
                Console.WriteLine("IteratedFib({0}) = {1}", i, FibonacciNumberIteration(i));
            }

            for (uint i = 1; i < 100; ++i)
            {
                Console.WriteLine("Fib({0}) = {1}", i, FibonacciNumber(i));
            }
        }
    }
}
