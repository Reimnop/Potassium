using System;

namespace Potassium.Threading
{
    public static class ThreadManager
    {
        public static int NumberOfWorkers => _numWorkers;

        private static int _numWorkers;

        private static Worker[] _workers;

        public static void InitWorkers(int numWorkers)
        {
            _numWorkers = numWorkers;
            _workers = new Worker[numWorkers];

            for (int i = 0; i < numWorkers; i++)
            {
                _workers[i] = new Worker();
            }
        }

        public static void QueueWorker(int workerIndex, Action action)
        {
            _workers[workerIndex].Queue(action);
        }

        public static void StartAllWorkers()
        {
            for (int i = 0; i < _numWorkers; i++)
            {
                _workers[i].StartExecuting();
            }
        }

        public static bool AllDone()
        {
            for (int i = 0; i < _numWorkers; i++)
            {
                if (!_workers[i].Finished)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
