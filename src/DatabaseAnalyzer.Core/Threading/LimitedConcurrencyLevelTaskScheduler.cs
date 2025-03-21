namespace DatabaseAnalyzer.Core.Threading;

// Copied from https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=net-9.0
#pragma warning disable

public sealed class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
{
    [ThreadStatic] private static bool CurrentThreadIsProcessingItems;

    private readonly LinkedList<Task> _tasks = new(); // protected by lock(_tasks)
    private int _delegatesQueuedOrRunning;
    public override int MaximumConcurrencyLevel { get; }

    public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
        }

        MaximumConcurrencyLevel = maxDegreeOfParallelism;
    }

    protected override void QueueTask(Task task)
    {
        // Add the task to the list of tasks to be processed.  If there aren't enough
        // delegates currently queued or running to process tasks, schedule another.
        lock (_tasks)
        {
            _tasks.AddLast(task);
            if (_delegatesQueuedOrRunning < MaximumConcurrencyLevel)
            {
                ++_delegatesQueuedOrRunning;
                NotifyThreadPoolOfPendingWork();
            }
        }
    }

    private void NotifyThreadPoolOfPendingWork()
    {
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            // Note that the current thread is now processing work items.
            // This is necessary to enable inlining of tasks into this thread.
            CurrentThreadIsProcessingItems = true;
            try
            {
                // Process all available items in the queue.
                while (true)
                {
                    Task item;
                    lock (_tasks)
                    {
                        // When there are no more items to be processed,
                        // note that we're done processing, and get out.
                        if (_tasks.Count == 0)
                        {
                            --_delegatesQueuedOrRunning;
                            break;
                        }

                        // Get the next item from the queue
                        item = _tasks.First.Value;
                        _tasks.RemoveFirst();
                    }

                    // Execute the task we pulled out of the queue
                    TryExecuteTask(item);
                }
            }
            // We're done processing items on the current thread
            finally
            {
                CurrentThreadIsProcessingItems = false;
            }
        }, null);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // If this thread isn't already processing a task, we don't support inlining
        if (!CurrentThreadIsProcessingItems)
        {
            return false;
        }

        // If the task was previously queued, remove it from the queue
        if (taskWasPreviouslyQueued)
            // Try to run the task.
        {
            if (TryDequeue(task))
            {
                return TryExecuteTask(task);
            }
            else
            {
                return false;
            }
        }

        return TryExecuteTask(task);
    }

    protected override bool TryDequeue(Task task)
    {
        lock (_tasks)
        {
            return _tasks.Remove(task);
        }
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        var lockTaken = false;
        try
        {
            Monitor.TryEnter(_tasks, ref lockTaken);
            if (lockTaken)
            {
                return _tasks;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(_tasks);
            }
        }
    }
    // Indicates whether the current thread is processing work items.

    // The list of tasks to be executed

    // The maximum concurrency level allowed by this scheduler.

    // Indicates whether the scheduler is currently processing work items.

    // Creates a new instance with the specified degree of parallelism.

    // Queues a task to the scheduler.

    // Inform the ThreadPool that there's work to be executed for this scheduler.

    // Attempts to execute the specified task on the current thread.

    // Attempt to remove a previously scheduled task from the scheduler.

    // Gets the maximum concurrency level supported by this scheduler.

    // Gets an enumerable of the tasks currently scheduled on this scheduler.
}
