using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scenarios.Services;

namespace Scenarios.Controllers
{
    public class CancelLegacyOperationController : Controller
    {
        [HttpGet("/legacy-cancellation-1")]
        public async Task<IActionResult> LegacyCancellationWithCancellationBad()
        {
            var service = new LegacyService();
            var timeout = TimeSpan.FromSeconds(10);

            var serviceTask = service.DoAsyncOperation();
            var delayTask = Task.Delay(timeout);

            var resultTask = await Task.WhenAny(serviceTask, delayTask);
            if (resultTask == delayTask)
            {
                // Operation cancelled
                throw new OperationCanceledException();
            }

            return Ok(await serviceTask);
        }

        [HttpGet("/legacy-cancellation-2")]
        public async Task<IActionResult> LegacyCancellationWithCancellationGood()
        {
            var service = new LegacyService();
            var timeout = TimeSpan.FromSeconds(10);

            using (var cts = new CancellationTokenSource())
            {
                var serviceTask = service.DoAsyncOperation();
                var delayTask = Task.Delay(timeout, cts.Token);

                var resultTask = await Task.WhenAny(serviceTask, delayTask);
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

                return Ok(await serviceTask);
            }
        }
    }
}
