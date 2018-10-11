using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scenarios.Services;

namespace Scenarios.Controllers
{
    public class AsyncOperationController : Controller
    {
        [HttpGet("/async-1")]
        public IActionResult BadBlocking1()
        {
            var service = new LegacyService();

            var result = service.DoOperationBlocking();

            return Ok(result);
        }

        [HttpGet("/async-2")]
        public IActionResult BadBlocking2()
        {
            var service = new LegacyService();

            var result = service.DoOperationBlocking2();

            return Ok(result);
        }

        [HttpGet("/async-3")]
        public IActionResult BadBlocking3()
        {
            var service = new LegacyService();

            var result = service.DoOperationBlocking3();

            return Ok(result);
        }

        [HttpGet("/async-4")]
        public IActionResult BadBlocking4()
        {
            var service = new LegacyService();

            var result = service.DoOperationBlocking4();

            return Ok(result);
        }

        [HttpGet("/async-5")]
        public IActionResult BadBlocking5()
        {
            var service = new LegacyService();

            var result = service.DoOperationBlocking5();

            return Ok(result);
        }

        [HttpGet("/async-6")]
        public IActionResult BadBlocking6()
        {
            var service = new LegacyService();

            var result = service.DoOperationBlocking6();

            return Ok(result);
        }

        [HttpGet("/async-7")]
        public IActionResult BadBlocking7()
        {
            var service = new LegacyService();

            var result = service.DoOperationBlocking7();

            return Ok(result);
        }

        [HttpGet("/async-7")]
        public async Task<IActionResult> BadBlocking8()
        {
            var service = new LegacyService();

            var result = await service.DoAsyncOverSyncOperation();

            return Ok(result);
        }

        [HttpGet("/async-8")]
        public async Task<IActionResult> GoodBlocking()
        {
            var service = new LegacyService();

            var result = await service.DoSyncOperationWithAsyncReturn();

            return Ok(result);
        }

        [HttpGet("/async-9")]
        public async Task<IActionResult> AsyncCall()
        {
            var service = new LegacyService();

            var result = await service.DoAsyncOperation();

            return Ok(result);
        }
    }
}
