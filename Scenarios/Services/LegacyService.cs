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
    }
}
