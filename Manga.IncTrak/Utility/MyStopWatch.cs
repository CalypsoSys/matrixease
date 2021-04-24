using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Utility
{
    class MyStopWatch
    {
#if DEBUG
        private Stack<Tuple<string, Stopwatch>> _stopWatches = new Stack<Tuple<string, Stopwatch>>();
#endif

        public static MyStopWatch StartNew(string description)
        {
            return new MyStopWatch(description);
        }

        private MyStopWatch(string description)
        {
#if DEBUG
            StartSubTime(description);
#endif
        }

        public void StartSubTime(string description)
        {
#if DEBUG
            _stopWatches.Push(Tuple.Create(description, Stopwatch.StartNew()));
#endif
        }

        public string StopSubTime()
        {
#if DEBUG
            if (_stopWatches.Count > 0)
            {
                var stopwatch = _stopWatches.Pop();
                stopwatch.Item2.Stop();
                return string.Format("{0} -- {1:00}:{2:00}:{3:00}.{4:00}", stopwatch.Item1,
                        stopwatch.Item2.Elapsed.Hours, stopwatch.Item2.Elapsed.Minutes, stopwatch.Item2.Elapsed.Seconds,
                        stopwatch.Item2.Elapsed.Milliseconds / 10);
            }
#endif

            return string.Format("N/A -- Error with stopwatch");
        }

        public string Stop()
        {
#if DEBUG
            bool mismatched = false;
            while (_stopWatches.Count > 1)
            {
                StopSubTime();
                mismatched = true;
            }

            return string.Format("{0}{1}", StopSubTime(), (mismatched ? " -- MISMATCHED" : ""));
#else
            return string.Empty;
#endif
        }
    }
}
