using System;
using System.Threading.Tasks;

namespace TVHeadEnd.TimeoutHelper
{
    public class TaskWithTimeoutRunner<T>
    {
        private readonly TimeSpan TIMEOUT;

        public TaskWithTimeoutRunner(TimeSpan timeout)
        {
            TIMEOUT = timeout;
        }

        public Task<TaskWithTimeoutResult<T>> RunWithTimeout(Task<T> task)
        {
            return Task.Factory.StartNew<TaskWithTimeoutResult<T>>(() =>
            {
                Task<TaskWithTimeoutResult<T>> outherTask = new Task<TaskWithTimeoutResult<T>>(() =>
                {
                    Task<TaskWithTimeoutResult<T>> longRunningTask = new Task<TaskWithTimeoutResult<T>>(() =>
                    {
                        TaskWithTimeoutResult<T> myTaskResult = new TaskWithTimeoutResult<T>();
                        myTaskResult.Result = task.Result;
                        myTaskResult.HasTimeout = false;
                        return myTaskResult;
                    }, TaskCreationOptions.LongRunning);

                    longRunningTask.Start();

                    if (longRunningTask.Wait(TIMEOUT))
                    {
                        return longRunningTask.Result;
                    }

                    // If we reach here we had an timeout
                    TaskWithTimeoutResult<T> timeoutResult = new TaskWithTimeoutResult<T>();
                    timeoutResult.Result = default(T);
                    timeoutResult.HasTimeout = true;
                    return timeoutResult;
                });

                outherTask.Start();
                outherTask.Wait();
                return outherTask.Result;
            });
        }
    }
}
