using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ch06
{
    class Program
    {
        #region Primes

        //Returns all the prime numbers in the range [start, end)
        public static IEnumerable<uint> PrimesInRange_Sequential(uint start, uint end)
        {
            List<uint> primes = new List<uint>();
            for (uint number = start; number < end; ++number)
            {
                if (IsPrime(number))
                {
                    primes.Add(number);
                }
            }
            return primes;
        }
        private static bool IsPrime(uint number)
        {
            //This is a very inefficient O(n) algorithm, but it will do for our expository purposes
            if (number == 2) return true;
            if (number % 2 == 0) return false;
            for (uint divisor = 3; divisor < number; divisor += 2)
            {
                if (number % divisor == 0) return false;
            }
            return true;
        }

        public static IEnumerable<uint> PrimesInRange_Threads(uint start, uint end)
        {
            List<uint> primes = new List<uint>();
            uint range = end - start;
            uint numThreads = (uint)Environment.ProcessorCount; //is this a good idea?
            uint chunk = range / numThreads; //hopefully, there is no remainder
            Thread[] threads = new Thread[numThreads];
            for (uint i = 0; i < numThreads; ++i)
            {
                uint chunkStart = start + i * chunk;
                uint chunkEnd = chunkStart + chunk;
                threads[i] = new Thread(() =>
                {
                    for (uint number = chunkStart; number < chunkEnd; ++number)
                    {
                        if (IsPrime(number))
                        {
                            lock (primes)
                            {
                                primes.Add(number);
                            }
                        }
                    }
                });
                threads[i].Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            return primes;
        }

        public static IEnumerable<uint> PrimesInRange_ThreadPool(uint start, uint end)
        {
            List<uint> primes = new List<uint>();
            const uint ChunkSize = 100;
            int completed = 0;
            ManualResetEvent allDone = new ManualResetEvent(initialState: false);
            uint chunks = (end - start) / ChunkSize; //again, this should divide evenly
            for (uint i = 0; i < chunks; ++i)
            {
                uint chunkStart = start + i * ChunkSize;
                uint chunkEnd = chunkStart + ChunkSize;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    for (uint number = chunkStart; number < chunkEnd; ++number)
                    {
                        if (IsPrime(number))
                        {
                            lock (primes)
                            {
                                primes.Add(number);
                            }
                        }
                    }
                    if (Interlocked.Increment(ref completed) == chunks)
                    {
                        allDone.Set();
                    }
                });
            }
            allDone.WaitOne();
            return primes;
        }

        public static IEnumerable<uint> PrimesInRange_ParallelFor(uint start, uint end)
        {
            List<uint> primes = new List<uint>();
            Parallel.For((long)start, (long)end, number =>
            {
                if (IsPrime((uint)number))
                {
                    lock (primes)
                    {
                        primes.Add((uint)number);
                    }
                }
            });
            return primes;
        }

        public static IEnumerable<uint> PrimesInRange_Aggregation(uint start, uint end)
        {
            List<uint> primes = new List<uint>();
            Parallel.For(3, 200000,
              () => new List<uint>(),        //initialize the local copy
              (i, pls, localPrimes) =>
              {    //single computation step, returns new local state
                  if (IsPrime((uint)i))
                  {
                      localPrimes.Add((uint)i);       //no synchronization necessary, thread-local state
                  }
                  return localPrimes;
              },
              localPrimes =>
              {              //combine the local lists to the global one
                  lock (primes)
                  {              //synchronization is required
                      primes.AddRange(localPrimes);
                  }
              }
            );
            return primes;
        }

        #endregion

        #region QuickSort

        public static void QuickSort_Sequential<T>(T[] items) where T : IComparable<T>
        {
            QuickSort_Sequential(items, 0, items.Length);
        }
        private static void QuickSort_Sequential<T>(T[] items, int left, int right) where T : IComparable<T>
        {
            if (left == right) return;
            int pivot = Partition(items, left, right);
            QuickSort_Sequential(items, left, pivot);
            QuickSort_Sequential(items, pivot + 1, right);
        }
        private static int Partition<T>(T[] items, int left, int right) where T : IComparable<T>
        {
            int pivotPos = (right - left) / 2; //often a random index between left and right is used
            T pivotValue = items[pivotPos];
            Swap(ref items[right - 1], ref items[pivotPos]);
            int store = left;
            for (int i = left; i < right - 1; ++i)
            {
                if (items[i].CompareTo(pivotValue) < 0)
                {
                    Swap(ref items[i], ref items[store]);
                    ++store;
                }
            }
            Swap(ref items[right - 1], ref items[store]);
            return store;
        }
        private static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        public static void QuickSort_Parallel<T>(T[] items) where T : IComparable<T>
        {
            QuickSort_Parallel(items, 0, items.Length);
        }
        private static void QuickSort_Parallel<T>(T[] items, int left, int right) where T : IComparable<T>
        {
            if (right - left < 2) return;
            int pivot = Partition(items, left, right);
            Task leftTask = Task.Run(() => QuickSort_Parallel(items, left, pivot));
            Task rightTask = Task.Run(() => QuickSort_Parallel(items, pivot + 1, right));
            Task.WaitAll(leftTask, rightTask);
        }

        public static void QuickSort_Parallel_Threshold<T>(T[] items) where T : IComparable<T>
        {
            QuickSort_Parallel_Threshold(items, 0, items.Length);
        }
        private static void QuickSort_Parallel_Threshold<T>(T[] items, int left, int right) where T : IComparable<T>
        {
            if (right - left < 2) return;
            int pivot = Partition(items, left, right);
            if (right - left > 500)
            {
                Parallel.Invoke(
                  () => QuickSort_Parallel_Threshold(items, left, pivot),
                  () => QuickSort_Parallel_Threshold(items, pivot + 1, right)
                );
            }
            else
            {
                QuickSort_Sequential(items, left, pivot);
                QuickSort_Sequential(items, pivot + 1, right);
            }
        }


        #endregion

        #region MatrixSum

        public static int MatrixSumSequential(int[,] matrix)
        {
            int sum = 0;
            int rows = matrix.GetUpperBound(0);
            int cols = matrix.GetUpperBound(1);
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < cols; ++j)
                {
                    sum += matrix[i, j];
                }
            }
            return sum;
        }

        public static int MatrixSumParallel(int[,] matrix)
        {
            int sum = 0;
            int rows = matrix.GetUpperBound(0);
            int cols = matrix.GetUpperBound(1);
            const int THREADS = 4;
            int chunk = rows / THREADS; //should divide evenly
            int[] localSums = new int[THREADS];
            Thread[] threads = new Thread[THREADS];
            for (int i = 0; i < THREADS; ++i)
            {
                int start = chunk * i;
                int end = chunk * (i + 1);
                int threadNum = i; //prevent the compiler from hoisting the variable in the lambda capture
                threads[i] = new Thread(() =>
                {
                    for (int row = start; row < end; ++row)
                    {
                        for (int col = 0; col < cols; ++col)
                        {
                            localSums[threadNum] += matrix[row, col];
                        }
                    }
                });
                threads[i].Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            unchecked { sum = localSums.Sum(); }
            return sum;
        }

        private static int[,] BuildMatrix(int rows, int cols)
        {
            Random random = new Random();
            int[,] matrix = new int[rows, cols];
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < cols; ++j)
                {
                    matrix[i, j] = random.Next();
                }
            }
            return matrix;
        }

        #endregion

        #region C++ AMP

        private static Tuple<float[], float[], float[]> BuildVectors(int size)
        {
            Random random = new Random();
            float[] first = new float[size];
            float[] second = new float[size];
            for (int i = 0; i < size; ++i)
            {
                first[i] = random.Next();
                second[i] = random.Next();
            }
            float[] result = new float[size];
            return new Tuple<float[], float[], float[]>(first, second, result);
        }
        private static Tuple<int[], int[], int[]> BuildMatrices(int m, int w, int n)
        {
            Random random = new Random();
            int[] first = new int[m*w];
            int[] second = new int[w*n];
            for (int i = 0; i < first.Length; ++i)
            {
                first[i] = random.Next();
            }
            for (int i = 0; i < second.Length; ++i)
            {
                second[i] = random.Next();
            }
            int[] result = new int[m*n];
            return new Tuple<int[], int[], int[]>(first, second, result);
        }

        [DllImport(@"..\..\..\Release\CppAMPDLL.dll")]
        public static extern void VectorAddExpPointwise_Sequential(float[] first, float[] second, float[] result, int length);
        
        [DllImport(@"..\..\..\Release\CppAMPDLL.dll")]
        public static extern void VectorAddExpPointwise_Parallel(float[] first, float[] second, float[] result, int length);

        [DllImport(@"..\..\..\Release\CppAMPDLL.dll")]
        public static extern void MatrixMultiplication_Sequential(int[] A, int m, int w, int[] B, int n, int[] C);
        
        [DllImport(@"..\..\..\Release\CppAMPDLL.dll")]
        public static extern void MatrixMultiplication_Simple(int[] A, int m, int w, int[] B, int n, int[] C);
        
        [DllImport(@"..\..\..\Release\CppAMPDLL.dll")]
        public static extern void MatrixMultiplication_Tiled(int[] A, int m, int w, int[] B, int n, int[] C);

        #endregion

        #region Async Methods

        class Textbox
        {
            public string Text { get; set; }
        }
        class City
        {
            public string Name { get; set; }
        }
        class Location
        {
            public City City { get; set; }
        }
        class Forecast
        {
            public string Summary { get; set; }
        }
        class MessageDialog
        {
            public MessageDialog(string text) { }
            public void Display() { }
            public Task DisplayAsync()
            {
                return Task.Run(() => Display());
            }
        }

        class LocationService : IDisposable
        {
            public Location GetCurrentLocation()
            {
                return new Location();
            }
            public Task<Location> GetCurrentLocationAsync()
            {
                return Task.Run(() => GetCurrentLocation());
            }

            public void Dispose()
            {
            }
        }

        class WeatherService : IDisposable
        {
            public Forecast GetForecast(City city)
            {
                return new Forecast();
            }
            public Task<Forecast> GetForecastAsync(City city)
            {
                return Task.Run(() => GetForecast(city));
            }

            public void Dispose()
            {
            }
        }

        private void Sync_UpdateClicked()
        {
            using (LocationService location = new LocationService())
            using (WeatherService weather = new WeatherService())
            {
                Location loc = location.GetCurrentLocation();
                Forecast forecast = weather.GetForecast(loc.City);
                MessageDialog msg = new MessageDialog(forecast.Summary);
                msg.Display();
            }
        }
        private void Async_UpdateClicked()
        {
            TaskScheduler uiScheduler = TaskScheduler.Current;
            LocationService location = new LocationService();
            Task<Location> locTask = location.GetCurrentLocationAsync();
            locTask.ContinueWith(_ =>
            {
                WeatherService weather = new WeatherService();
                Task<Forecast> forTask = weather.GetForecastAsync(locTask.Result.City);
                forTask.ContinueWith(__ =>
                {
                    MessageDialog message = new MessageDialog(forTask.Result.Summary);
                    Task msgTask = message.DisplayAsync();
                    msgTask.ContinueWith(___ =>
                    {
                        weather.Dispose();
                        location.Dispose();
                    });
                }, uiScheduler);
            });
        }

        private Forecast[] GetForecastForAllCities(City[] cities)
        {
            Forecast[] forecasts = new Forecast[cities.Length];
            using (WeatherService weather = new WeatherService())
            {
                for (int i = 0; i < cities.Length; ++i)
                {
                    forecasts[i] = weather.GetForecast(cities[i]);
                }
            }
            return forecasts;
        }
        private Task<Forecast[]> GetForecastsForAllCitiesAsync(City[] cities)
        {
            if (cities.Length == 0)
            {
                return Task.Run(() => new Forecast[0]);
            }
            WeatherService weather = new WeatherService();
            Forecast[] forecasts = new Forecast[cities.Length];
            return GetForecastHelper(weather, 0, cities, forecasts).ContinueWith(_ => forecasts);
        }
        private Task GetForecastHelper(WeatherService weather, int i, City[] cities, Forecast[] forecasts)
        {
            if (i >= cities.Length) return Task.Run(() => { });
            Task<Forecast> forecast = weather.GetForecastAsync(cities[i]);
            forecast.ContinueWith(task =>
            {
                forecasts[i] = task.Result;
                GetForecastHelper(weather, i + 1, cities, forecasts);
            });
            return forecast;
        }

        private Textbox cityTextBox = new Textbox();
        private async void Await_UpdateClicked()
        {
            using (LocationService location = new LocationService())
            {
                Task<Location> locTask = location.GetCurrentLocationAsync();
                Location loc = await locTask;
                cityTextBox.Text = loc.City.Name;
            }
        }
        private async Task<Forecast[]> GetForecastForAllCitiesAwait(City[] cities)
        {
            Forecast[] forecasts = new Forecast[cities.Length];
            using (WeatherService weather = new WeatherService())
            {
                for (int i = 0; i < cities.Length; ++i)
                {
                    forecasts[i] = await weather.GetForecastAsync(cities[i]);
                }
            }
            return forecasts;
        }

        #endregion

        #region Interlocked Operations and CAS

        public static void InterlockedMultiplyInPlace(ref int x, int y)
        {
            int temp, mult;
            do
            {
                temp = x;
                mult = temp * y;
            } while (Interlocked.CompareExchange(ref x, mult, temp) != temp);
        }
        public static void DoWithCAS<T>(ref T location, Func<T, T> generator) where T : class
        {
            T temp, replace;
            do
            {
                temp = location;
                replace = generator(temp);
            } while (Interlocked.CompareExchange(ref location, replace, temp) != temp);
        }
        public class SpinLock
        {
            private volatile int locked;
            public void Acquire()
            {
                while (Interlocked.CompareExchange(ref locked, 1, 0) != 0) ;
            }
            public void Release()
            {
                locked = 0;
            }
        }

        public class LockFreeStack<T>
        {
            private class Node
            {
                public T Data;
                public Node Next;
            }
            private Node head;
            public void Push(T element)
            {
                Node node = new Node { Data = element };
                DoWithCAS(ref head, h =>
                {
                    node.Next = h;
                    return node;
                });
            }
            public bool TryPop(out T element)
            {
                //DoWithCAS does not work here because we need early termination semantics
                Node node;
                do
                {
                    node = head;
                    if (node == null)
                    {
                        element = default(T);
                        return false; //bail out – nothing to return
                    }
                } while (Interlocked.CompareExchange(ref head, node.Next, node) != node);
                element = node.Data;
                return true;
            }
        }

        #endregion

        private static void Measure(Action what, string description)
        {
            const int ITERATIONS = 5;
            double[] elapsed = new double[ITERATIONS];
            for (int i = 0; i < ITERATIONS; ++i)
            {
                Stopwatch sw = Stopwatch.StartNew();
                what();
                elapsed[i] = sw.ElapsedMilliseconds;
            }
            Console.WriteLine("{0} took {1}ms on average", description, elapsed.Skip(1).Average());
        }
        private static void Measure<T>(Func<T> setup, Action<T> measurement, string description)
        {
            T state = setup();
            Measure(() => measurement(state), description);
        }
        private static void Repeat(int times, Action action)
        {
            for (int i = 0; i < times; ++i)
                action();
        }

        static void Main(string[] args)
        {
            Random rnd = new Random();

            Measure(() => PrimesInRange_Sequential(100, 200000), "PrimesInRange_Sequential(100, 200000)");
            Measure(() => PrimesInRange_Threads(100, 200000), "PrimesInRange_Threads(100, 200000)");
            Measure(() => PrimesInRange_ThreadPool(100, 200000), "PrimesInRange_ThreadPool(100, 200000)");
            Measure(() => PrimesInRange_ParallelFor(100, 200000), "PrimesInRange_ParallelFor(100, 200000)");
            Measure(() => PrimesInRange_Aggregation(100, 200000), "PrimesInRange_Aggregation(100, 200000)");

            //Measure(() => Enumerable.Range(0, 1000000).Select(n => rnd.Next()).ToArray(), QuickSort_Sequential, "QuickSort_Sequential (1,000,000 including allocation)");
            //Measure(() => Enumerable.Range(0, 1000000).Select(n => rnd.Next()).ToArray(), QuickSort_Parallel, "QuickSort_Parallel (1,000,000 including allocation)");
            //Measure(() => Enumerable.Range(0, 1000000).Select(n => rnd.Next()).ToArray(), QuickSort_Parallel_Threshold, "QuickSort_Parallel_Threshold (1,000,000 including allocation)");

            //Measure(() => BuildMatrix(2000, 2000), matrix => Repeat(25, () => MatrixSumSequential(matrix)), "MatrixSumSequential (2000x2000)");
            //Measure(() => BuildMatrix(2000, 2000), matrix => Repeat(25, () => MatrixSumParallel(matrix)), "MatrixSumParallel (2000x2000)");

            //Measure(() => BuildVectors(1000000), data => VectorAddExpPointwise_Sequential(data.Item1, data.Item2, data.Item3, data.Item1.Length), "VectorAddExpPointwise_Sequential (10,000,000)");
            //Measure(() => BuildVectors(1000000), data => VectorAddExpPointwise_Parallel(data.Item1, data.Item2, data.Item3, data.Item1.Length), "VectorAddExpPointwise_Parallel (10,000,000)");

            //Measure(() => BuildMatrices(1024, 1024, 1024), data => MatrixMultiplication_Sequential(data.Item1, 1024, 1024, data.Item2, 1024, data.Item3), "MatrixMultiplication_Sequential (1024x1024x1024)");
            //Measure(() => BuildMatrices(1024, 1024, 1024), data => MatrixMultiplication_Simple(data.Item1, 1024, 1024, data.Item2, 1024, data.Item3), "MatrixMultiplication_Simple (1024x1024x1024)");
            //Measure(() => BuildMatrices(1024, 1024, 1024), data => MatrixMultiplication_Tiled(data.Item1, 1024, 1024, data.Item2, 1024, data.Item3), "MatrixMultiplication_Tiled (1024x1024x1024)");
        }
    }
}
