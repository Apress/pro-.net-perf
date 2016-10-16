using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ch05
{
    class BasicStack<T>
    {
        private T[] items;
        private int topIndex;

        public BasicStack(int capacity = 42)
        {
            items = new T[capacity];
        }
        public void Push(T item)
        {
            items[topIndex++] = item;
        }
        public T Pop()
        {
            return items[--topIndex];
        }
    }

    class Program
    {
        private static void Measure(Action what, string description)
        {
            const int ITERATIONS = 1;
            double[] elapsed = new double[ITERATIONS];
            for (int i = 0; i < ITERATIONS; ++i)
            {
                Stopwatch sw = Stopwatch.StartNew();
                what();
                elapsed[i] = sw.ElapsedMilliseconds;
            }
            Console.WriteLine("{0} took {1}ms on average", description, elapsed.Average());
        }
        private static void Measure<T>(Func<T> setup, Action<T> measurement, string description)
        {
            T state = setup();
            Measure(() => measurement(state), description);
        }

        static volatile int sum = 0;

        static int N = 2048;

        static int[,] BuildMatrix()
        {
            int[,] m = new int[N, N];
            Random r = new Random(Environment.TickCount);
            for (int i = 0; i < N; ++i)
                for (int j = 0; j < N; ++j)
                    m[i, j] = r.Next();
            return m;
        }
        static int[,] MultiplyNaive(int[,] A, int[,] B)
        {
            int[,] C = new int[N, N];
            for (int i = 0; i < N; ++i)
                for (int j = 0; j < N; ++j)
                    for (int k = 0; k < N; ++k)
                        C[i, j] += A[i, k] * B[k, j];
            return C;
        }
        static int[,] MultiplyBlocked(int[,] A, int[,] B, int bs)
        {
            int[,] C = new int[N, N];
            for (int ii = 0; ii < N; ii += bs)
                for (int jj = 0; jj < N; jj += bs)
                    for (int kk = 0; kk < N; kk += bs)
                    {
                        for (int i = ii; i < ii + bs; ++i)
                        {
                            for (int j = jj; j < jj + bs; ++j)
                            {
                                for (int k = kk; k < kk + bs; ++k)
                                    C[i, j] += A[i, k] * B[k, j];
                            }
                        }
                    }
            return C;
        }

        static void Main(string[] args)
        {
            #region BasicStack generics example

            BasicStack<string> stringStack = new BasicStack<string>();
            stringStack.Push("Hello");
            stringStack.Pop();
            BasicStack<int[]> intArrStack = new BasicStack<int[]>();
            intArrStack.Push(new[] {14});
            intArrStack.Pop();
            BasicStack<int> intStack = new BasicStack<int>();
            intStack.Push(42);
            intStack.Pop();
            BasicStack<double> doubleStack = new BasicStack<double>();
            doubleStack.Push(1.0);
            doubleStack.Pop();
            Console.ReadLine();

            #endregion

            #region Matrix multiplication

            int[,] A;
            int[,] B;
            Stopwatch sw;
            int[,] C;

            A = BuildMatrix();
            B = BuildMatrix();
            sw = Stopwatch.StartNew();
            C = MultiplyNaive(A, B);
            Console.WriteLine("Naive: " + sw.ElapsedMilliseconds);

            for (int bs = 4; bs <= N; bs *= 2)
            {
                A = BuildMatrix();
                B = BuildMatrix();
                sw = Stopwatch.StartNew();
                C = MultiplyBlocked(A, B, bs);
                Console.WriteLine("Blocked (bs=" + bs + "): " + sw.ElapsedMilliseconds);
            }

            #endregion

            #region LinkedList vs Array

            Measure(() => new LinkedList<int>(Enumerable.Range(0, 20000000)), numbers =>
            {
                for (LinkedListNode<int> curr = numbers.First; curr != null; curr = curr.Next)
                    sum += curr.Value;
            }, "LinkedList<int>");
            Measure(() => Enumerable.Range(0, 20000000).ToList(), numbers =>
            {
                foreach (int number in numbers)
                    sum += number;
            }, "List<int>");
            Measure(() => Enumerable.Range(0, 20000000).ToArray(), numbers =>
            {
                for (int i = 0; i < numbers.Length; ++i)
                    sum += numbers[i];
            }, "int[]");
            Console.ReadLine();

            #endregion
        }
    }
}
