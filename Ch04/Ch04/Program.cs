using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ch04
{
    #region Object Pooling

    public class Pool<T>
    {
        private ConcurrentBag<T> pool = new ConcurrentBag<T>();
        private Func<T> objectFactory;

        public Pool(Func<T> factory)
        {
            objectFactory = factory;
        }
        public T GetInstance()
        {
            T result;
            if (!pool.TryTake(out result))
            {
                result = objectFactory();
            }
            return result;
        }
        public void ReturnToPool(T instance)
        {
            pool.Add(instance);
        }
    }

    public class PoolableObjectBase<T> : IDisposable
    {
        private static Pool<T> pool = new Pool<T>();

        public void Dispose()
        {
            pool.ReturnToPool(this);
        }
        ~PoolableObjectBase()
        {
            GC.ReRegisterForFinalize(this);
            pool.ReturnToPool(this);
        }
    }

    #endregion

    #region GC Notifications

    public class GCWatcher
    {
        private Thread watcherThread;

        public event EventHandler GCApproaches;
        public event EventHandler GCComplete;

        public void Watch()
        {
            GC.RegisterForFullGCNotification(50, 50);
            watcherThread = new Thread(() =>
            {
                while (true)
                {
                    GCNotificationStatus status = GC.WaitForFullGCApproach();
                    //Omitted error handling code here
                    if (GCApproaches != null)
                    {
                        GCApproaches(this, EventArgs.Empty);
                    }
                    status = GC.WaitForFullGCComplete();
                    //Omitted error handling code here
                    if (GCComplete != null)
                    {
                        GCComplete(this, EventArgs.Empty);
                    }
                }
            });
            watcherThread.IsBackground = true;
            watcherThread.Start();
        }

        public void Cancel()
        {
            GC.CancelFullGCNotification();
            watcherThread.Join();
        }
    }

    #endregion

    #region Various types of roots

    class Widget
    {
        public virtual void Use()
        {
        }
    }

    class Customer
    {
        public Order LastOrder { get; set; }
    }
    class Order { }

    class Employee
    {
        public void OnEvent(object sender, EventArgs e) { }

        ~Employee() { Thread.Sleep(10000); }
    }

    #endregion

    class Program
    {
        static EventHandler MyEvent;

        static void Main(string[] args)
        {
            Console.WriteLine(GCSettings.IsServerGC);
            Console.WriteLine(GCSettings.LatencyMode);
            GCSettings.LatencyMode = GCLatencyMode.Interactive;

            MyEvent += new Employee().OnEvent;
            Employee e = new Employee();
            Employee e2 = new Employee();
            Console.ReadLine();
            Console.WriteLine(e.GetType().ToString());
            GC.Collect();
            Console.ReadLine();

            Customer customer = new Customer();
            GC.Collect();
            GC.Collect();
            customer.LastOrder = new Order();
            Console.ReadLine();

            Widget a = new Widget();
            a.Use();
            a.Use();
            Widget b = new Widget();
            b.Use();
            b.Use();
            a.Use();
            Console.ReadLine();
        }
    }
}
