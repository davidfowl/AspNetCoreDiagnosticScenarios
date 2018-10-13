using System;
using System.Threading;
using System.Threading.Tasks;

namespace Scenarios.Services
{
    /// <summary>
    /// The <see cref="LegacyService"/> shows to various ways people attempt to blocking code over an async API. There is NO 
    /// good way to turn asynchronous code into synchronous code. All of these blocking calls can cause thread pool starvation.
    /// </summary>
    public class LegacyService
    {
        public string DoOperationBlocking()
        {
            return Task.Run(() => DoAsyncOperation()).Result;
        }

        public string DoOperationBlocking2()
        {
            return Task.Run(() => DoAsyncOperation()).GetAwaiter().GetResult();
        }

        public string DoOperationBlocking3()
        {
            return Task.Run(() => DoAsyncOperation().Result).Result;
        }

        public string DoOperationBlocking4()
        {
            return Task.Run(() => DoAsyncOperation().GetAwaiter().GetResult()).GetAwaiter().GetResult();
        }

        public string DoOperationBlocking5()
        {
            return DoAsyncOperation().Result;
        }

        public string DoOperationBlocking6()
        {
            return DoAsyncOperation().GetAwaiter().GetResult();
        }

        public string DoOperationBlocking7()
        {
            var task = DoAsyncOperation();
            task.Wait();
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// DoAsyncOperation is a truly async operation. This is the recommended way to do asynchronous calls.
        /// </summary>
        /// <returns></returns>
        public async Task<string> DoAsyncOperation()
        {
            var random = new Random();

            // Mimick some asynchrous activity
            await Task.Delay(random.Next(10) * 1000);

            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// DoAsyncOverSyncOperation is wasteful. It uses a thread pool thread to return an easily computed value.
        /// The preferred approach is DoSyncOperationWithAsyncReturn.
        /// </summary>
        /// <returns></returns>
        public Task<string> DoAsyncOverSyncOperation()
        {
            return Task.Run(() => Guid.NewGuid().ToString());
        }

        /// <summary>
        /// This is the recommended way to return a Task for an already computed result. There's no need to use a thread pool thread,
        /// just to return a Task.
        /// </summary>
        /// <returns></returns>
        public Task<string> DoSyncOperationWithAsyncReturn()
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// DoAsyncOperationOverLegacyBad shows an async operation that does not properly clean up references to the canceallation token.
        /// </summary>
        public Task<string> DoAsyncOperationOverLegacyBad(CancellationToken cancellationToken)
        {
            // The following TaskCompletionSource hasn't been creating with the TaskCreationOptions.RunContinuationsAsynchronously
            // option. This means that the calling code will resume in the OnCompleted callback. This has a couple of consequences
            // 1. It will extend the lifetime of these objects since they will be on the stack when user code is resumed.
            // 2. If the calling code blocks, it could *steal* the thread from the LegacyAsyncOperation.
            var tcs = new TaskCompletionSource<string>();

            var operation = new LegacyAsyncOperation();

            if (cancellationToken.CanBeCanceled)
            {
                // CancellationToken.Register returns a CancellationTokenRegistration that needs to be disposed.
                // If this isn't disposed, it will stay around in the CancellationTokenSource until the
                // backing CancellationTokenSource is disposed.
                cancellationToken.Register(state =>
                {
                    ((LegacyAsyncOperation)state).Cancel();
                },
                operation);
            }

            // Not removing the event handler can result in a memory leak
            // this object is referenced by the callback which itself isn't cleaned up until
            // the token is disposed or the registration is disposed.
            operation.Completed += OnCompleted;

            operation.Start();

            return tcs.Task;

            void OnCompleted(string result, bool cancelled)
            {
                if (cancelled)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                else
                {
                    tcs.TrySetResult(result);
                }
            }
        }

        public Task<string> DoAsyncOperationOverLegacy(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            var operation = new LegacyAsyncOperation();

            var registration = default(CancellationTokenRegistration);

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(state =>
                {
                    ((LegacyAsyncOperation)state).Cancel();
                },
                operation);
            }

            operation.Completed += OnCompleted;

            operation.Start();

            return tcs.Task;

            void OnCompleted(string result, bool cancelled)
            {
                registration.Dispose();

                operation.Completed -= OnCompleted;

                if (cancelled)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                else
                {
                    tcs.TrySetResult(result);
                }
            }
        }

        /// <summary>
        /// Pretends to be a legacy async operation that doesn't natively support Task
        /// </summary>
        private class LegacyAsyncOperation
        {
            private Timer _timer;

            public Action<string, bool> Completed;

            private bool _cancelled;

            public void Start()
            {
                _timer = new Timer(OnCompleted, null, new Random().Next(10) * 1000, Timeout.Infinite);
            }

            private void OnCompleted(object state)
            {
                var cancelled = _cancelled;
                _cancelled = false;

                Completed(Guid.NewGuid().ToString(), cancelled);

                _timer.Dispose();
            }

            public void Cancel()
            {
                _cancelled = true;
            }
        }
    }
}
