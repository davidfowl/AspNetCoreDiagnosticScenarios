using System;
using System.Threading;
using System.Threading.Tasks;

namespace Scenarios.Services
{
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

        public async Task<string> DoAsyncOperation()
        {
            var random = new Random();

            // Mimick some asynchrous activity
            await Task.Delay(random.Next(10) * 1000);

            return Guid.NewGuid().ToString();
        }

        public Task<string> DoAsyncOverSyncOperation()
        {
            return Task.Run(() => Guid.NewGuid().ToString());
        }

        public Task<string> DoSyncOperationWithAsyncReturn()
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        public Task<string> DoAsyncOperationOverLegacyBad(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>();

            var operation = new LegacyAsyncOperation();

            if (cancellationToken.CanBeCanceled)
            {
               cancellationToken.Register(state =>
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
