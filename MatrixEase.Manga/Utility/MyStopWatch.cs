using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public class MyStopWatch
    {
#if DEBUG
        private Stack<Tuple<MyPerformance, Stopwatch>> _stopWatches = new Stack<Tuple<MyPerformance, Stopwatch>>();
#endif

        public static MyStopWatch StartNew(string name, string description)
        {
            return new MyStopWatch(name, description);
        }

        private MyStopWatch(string name, string description)
        {
#if DEBUG
            StartSubTime(name, description);
#endif
        }

        public void StartSubTime(string name, string description)
        {
#if DEBUG
            _stopWatches.Push(Tuple.Create(new MyPerformance(name, description), Stopwatch.StartNew()));
#endif
        }

        public MyPerformance StopSubTime()
        {
#if DEBUG
            if (_stopWatches.Count > 0)
            {
                var stopwatch = _stopWatches.Pop();
                stopwatch.Item2.Stop();
                stopwatch.Item1.SetValue(string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        stopwatch.Item2.Elapsed.Hours, stopwatch.Item2.Elapsed.Minutes, stopwatch.Item2.Elapsed.Seconds,
                        stopwatch.Item2.Elapsed.Milliseconds / 10));

                return stopwatch.Item1;
            }
#endif

            return new MyPerformance("stopwatch_error", 0, "N/A -- Error with stopwatch");
        }

        public MyPerformance Stop()
        {
#if DEBUG
            bool mismatched = false;
            while (_stopWatches.Count > 1)
            {
                StopSubTime();
                mismatched = true;
            }

            MyPerformance perf = StopSubTime();
            if ( mismatched)
            {
                perf.AppendDescription(" -- MISMATCHED --");
            }

            return perf;
#else
            return new MyPerformance(string.Empty, null, string.Empty);
#endif
        }
    }
}
