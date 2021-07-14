using System;
using System.Collections.Generic;
using System.Threading;

namespace Potassium.Threading
{
    public class Worker : IDisposable
    {
        public bool Finished => _finished;

        private Thread _thread;
        private Queue<Action> _workQueue = new Queue<Action>();

        private bool _finished = true;

        private bool _running = true;

        private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        public Worker()
        {
            ThreadStart threadStart = new ThreadStart(WaitUntilHasWork);

            _thread = new Thread(threadStart);
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void WaitUntilHasWork()
        {
            while (_running)
            {
                _manualResetEvent.WaitOne();
                if (_workQueue.Count > 0)
                {
                    DoAllWork();
                    _finished = true;
                }
                _manualResetEvent.Reset();
            }
        }

        private void DoAllWork()
        {
            while (_workQueue.Count > 0)
            {
                Action action = _workQueue.Dequeue();
                action?.Invoke();
            }
        }

        public void Queue(Action action)
        {
            _workQueue.Enqueue(action);
            _finished = false;
        }

        public void StartExecuting()
        {
            _manualResetEvent.Set();
        }

        public void Dispose()
        {
            _running = false;
        }
    }
}
