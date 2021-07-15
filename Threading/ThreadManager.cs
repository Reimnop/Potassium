using BepInEx.Logging;
using System;
using UnityEngine.Diagnostics;

namespace Potassium.Threading
{
    public static class ThreadManager
    {
        public static int NumberOfWorkers => _numWorkers;

        private static int _numWorkers;

        private static Worker[] _workers;

        private static ManualLogSource _logSource;

        public static void InitWorkers(int numWorkers)
        {
            _numWorkers = numWorkers;
            _workers = new Worker[numWorkers];

            for (int i = 0; i < numWorkers; i++)
            {
                _workers[i] = new Worker();
            }

            _logSource = Logger.CreateLogSource("Threading");
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
                Worker worker = _workers[i];

                if (!worker.Running && worker.ExceptionThrown)
                {
                    _logSource.LogError($"Exception thrown on worker {i}: {worker.ThrownException.Message}");
                    _logSource.LogError($"Stack trace: {worker.ThrownException.StackTrace}");
                    Utils.ForceCrash(ForcedCrashCategory.Abort);
                }
                else if (!worker.Running)
                {
                    continue;
                }

                if (!worker.Finished)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
