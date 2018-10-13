using System;
using System.Threading;

namespace Scenarios.Services
{
    /// <summary>
    /// Creating this object without disposing it will cause a memory leak. The object graph will look like this
    /// Timer -> TimerHolder -> TimerQueueTimer ->  EphemeralOperation -> Timer -> ...
    /// The timer holds onto the state which in turns holds onto the Timer, we have a circular reference.
    /// The GC will not clean these up even if it is unreferenced. It needs to be explicitly disposed in order to avoid the leak.
    /// </summary>
    public class EphemeralOperation : IDisposable
    {
        private Timer _timer;
        private int _ticks;

        public EphemeralOperation()
        {
            _timer = new Timer(state =>
            {
                _ticks++;
            },
            null,
            1000,
            1000);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }

    /// <summary>
    /// This fixes the cycle by using a WeakReference to the state object. The object graph now looks like this:
    /// Timer -> TimerHolder -> TimerQueueTimer -> WeakReference&lt;EphemeralOperation&gt; -> Timer -> ...
    /// If EphemeralOperation2 falls out of scope, the timer should be released.
    /// </summary>
    public class EphemeralOperation2 : IDisposable
    {
        private Timer _timer;
        private int _ticks;

        public EphemeralOperation2()
        {
            _timer = new Timer(OnTimerCallback,
            new WeakReference<EphemeralOperation2>(this),
            1000,
            1000);
        }

        private static void OnTimerCallback(object state)
        {
            var thisRef = (WeakReference<EphemeralOperation2>)state;
            if (thisRef.TryGetTarget(out var op))
            {
                op._ticks++;
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
