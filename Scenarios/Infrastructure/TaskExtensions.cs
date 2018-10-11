namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        /// <summary>
        /// The timer won't be disposed until this token triggers. If it is a long lived token
        /// that may result in memory leaks and timer queue exhaustion!
        /// </summary>
        public static async Task<T> WithCancellationBad<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var delayTask = Task.Delay(-1, cancellationToken);

            var resultTask = await Task.WhenAny(task, delayTask);
            if (resultTask == delayTask)
            {
                // Operation cancelled
                throw new OperationCanceledException();
            }

            return await task;
        }

        /// <summary>
        /// This properly registers and unregisters the token when one of the operations completes
        /// </summary>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            // This disposes the registration as soon as one of the tasks trigger
            using (cancellationToken.Register(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
            },
            tcs))
            {
                var resultTask = await Task.WhenAny(task, tcs.Task);
                if (resultTask == tcs.Task)
                {
                    // Operation cancelled
                    throw new OperationCanceledException(cancellationToken);
                }

                return await task;
            }
        }

        /// <summary>
        /// This method does not cancel the timer even if the operation successfuly completes.
        /// This means you could end up with timer queue flooding!
        /// </summary>
        public static async Task<T> TimeoutAfterBad<T>(this Task<T> task, TimeSpan timeout)
        {
            var delayTask = Task.Delay(timeout);

            var resultTask = await Task.WhenAny(task, delayTask);
            if (resultTask == delayTask)
            {
                // Operation cancelled
                throw new OperationCanceledException();
            }

            return await task;
        }

        /// <summary>
        /// This method cancels the timer if the operation succesfully completes.
        /// </summary>
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var delayTask = Task.Delay(timeout, cts.Token);

                var resultTask = await Task.WhenAny(task, delayTask);
                if (resultTask == delayTask)
                {
                    // Operation cancelled
                    throw new OperationCanceledException();
                }
                else
                {
                    // Cancel the timer task so that it does not fire
                    cts.Cancel();
                }

                return await task;
            }
        }
    }
}
