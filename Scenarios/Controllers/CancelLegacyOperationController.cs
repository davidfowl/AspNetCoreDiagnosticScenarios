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
        public async Task<IActionResult> LegacyCancellationWithTimeoutBad()
        {
            var service = new LegacyService();
            var timeout = TimeSpan.FromSeconds(10);

            var result = await service.DoAsyncOperation().TimeoutAfterBad(timeout);

            return Ok(result);
        }

        [HttpGet("/legacy-cancellation-2")]
        public async Task<IActionResult> LegacyCancellationWithTimeoutGood()
        {
            var service = new LegacyService();
            var timeout = TimeSpan.FromSeconds(10);

            var result = await service.DoAsyncOperation().TimeoutAfter(timeout);

            return Ok(result);
        }

        [HttpGet("/legacy-cancellation-3")]
        public async Task<IActionResult> LegacyCancellationWithCancellationBad()
        {
            var service = new LegacyService();

            var result = await service.DoAsyncOperation().WithCancellationBad(HttpContext.RequestAborted);

            return Ok(result);
        }

        [HttpGet("/legacy-cancellation-4")]
        public async Task<IActionResult> LegacyCancellationWithCancellationGood()
        {
            var service = new LegacyService();

            var result = await service.DoAsyncOperation().WithCancellation(HttpContext.RequestAborted);

            return Ok(result);
        }
    }
}
