using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ch02
{
    public class CustomEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords Loop = (EventKeywords)1;
            public const EventKeywords Method = (EventKeywords)2;
        }

        [Event(1, Level=EventLevel.Verbose, Keywords=Keywords.Loop, Message="Loop {0} iteration {1}")]
        public void LoopIteration(string loopTitle, int iteration)
        {
            WriteEvent(1, loopTitle, iteration);
        }
        [Event(2, Level=EventLevel.Informational, Keywords=Keywords.Loop, Message="Loop {0} done")]
        public void LoopDone(string loopTitle)
        {
            WriteEvent(2, loopTitle);
        }
        [Event(3, Level=EventLevel.Informational, Keywords=Keywords.Method, Message="Method {0} done")]
        public void MethodDone([CallerMemberName] string methodName = null)
        {
            WriteEvent(3, methodName);
        }
    }

    class Program
    {
        private static CustomEventSource log;

        static void SomeMethod()
        {
            for (int i = 0; i < 10; ++i)
            {
                Thread.Sleep(50);
                log.LoopIteration("MainLoop", i);
            }
            log.LoopDone("MainLoop");
            Thread.Sleep(100);
            log.MethodDone();
        }

        static int InstrumentedMethod(int param)
        {
            List<int> evens = new List<int>();
            for (int i = 0; i < param; ++i)
            {
                if (i % 2 == 0)
                    evens.Add(i);
            }
            return evens.Count;
        }

        static void Main(string[] args)
        {
            string theLock = "TheLock";
            for (int i = 0; i < 10; ++i)
            {
                int copy = i;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    lock (theLock)
                    {
                        Thread.Sleep(100);
                        Console.WriteLine("Thread pool thread #{0} done", copy);
                    }
                });
            }
            Console.ReadLine();

            Console.WriteLine(InstrumentedMethod(178));

            string manifest = EventSource.GenerateManifest(typeof(CustomEventSource), Assembly.GetExecutingAssembly().Location);
            File.WriteAllText("Ch02.man", manifest);
            Console.ReadLine();
            log = new CustomEventSource();
            SomeMethod();
        }
    }
}
