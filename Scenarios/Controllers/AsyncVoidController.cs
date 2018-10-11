using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Scenarios.Controllers
{
    public class AsyncVoidController : Controller
    {
        [HttpGet("/async-void-1")]
        public async void Get()
        {
            await Task.Delay(1000);

            // THIS will crash the process since we're writing after the response has completed on a background thread
            await Response.WriteAsync("Hello World");
        }

        [HttpGet("/async-void-2")]
        public async Task BrokenWebSockets()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

                // This is broken because we're not holding the request open until the WebSocket is closed!
                _ = Echo(ws);
            }
        }

        private async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
