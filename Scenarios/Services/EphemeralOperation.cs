using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public EphemeralOperation()
        {
            _timer = new Timer(OnTimerCallback, this, 1000, 1000);
        }

        private static void OnTimerCallback(object state)
        {

        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
