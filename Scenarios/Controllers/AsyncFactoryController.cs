using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scenarios.Services;

namespace Scenarios.Controllers
{
    public class AsyncFactoryController : Controller
    {
        /// <summary>
        /// This action is problematic because it tries to resolve the remote connection from the DI container
        /// which requires an asynchronous operation. See Startup.cs for the registration of RemoteConnection.
        /// </summary>
        [HttpGet("/async-di-1")]
        public async Task<IActionResult> PublishAsync([FromServices]RemoteConnection remoteConnection)
        {
            await remoteConnection.PublishAsync("group", "hello");

            return Accepted();
        }

        [HttpGet("/async-di-2")]
        public async Task<IActionResult> PublishAsync([FromServices]RemoteConnectionFactory remoteConnectionFactory)
        {
            // This doesn't have the dead lock issue but it makes a new connection every time
            var connection = await remoteConnectionFactory.ConnectAsync();

            await connection.PublishAsync("group", "hello");

            // Dispose the connection we created
            await connection.DisposeAsync();

            return Accepted();
        }

        [HttpGet("/async-di-3")]
        public async Task<IActionResult> PublishAsync([FromServices]LoggingRemoteConnection remoteConnection)
        {
            // This doesn't have the dead lock issue but it makes a new connection every time
            await remoteConnection.PublishAsync("group", "hello");

            return Accepted();
        }

        /// <summary>
        /// This is the cleanest pattern for dealing with async construction. The implementation of the connection is a bit
        /// more complicated but consumption looks like the first method that takes RemoteConnection and it is actually safe.
        /// </summary>
        [HttpGet("/async-di-4")]
        public async Task<IActionResult> PublishAsync([FromServices]LazyRemoteConnection remoteConnection)
        {
            await remoteConnection.PublishAsync("group", "hello");

            return Accepted();
        }
    }
}
