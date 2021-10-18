using System.Diagnostics;
using System.Net;

var numRequests = int.Parse(args[0]);
var url = args[1];

var client = new HttpClient(new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromDays(1) })
{
    Timeout = TimeSpan.FromDays(1)
};
var buffer = File.ReadAllBytes("pokemon.json");

Console.WriteLine($"Sending {numRequests} to {url}");

var sw = Stopwatch.StartNew();

var tasks = new Task[numRequests];
for (int i = 0; i < numRequests; i++)
{
    var req = new HttpRequestMessage(HttpMethod.Post, url)
    {
        Content = new SlowContent(buffer),
    };
    req.Content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
    tasks[i] = client.SendAsync(req);
}

Exception? exception = null;

try
{
    await Task.WhenAll(tasks);
}
catch (Exception ex)
{
    exception = ex;
}

sw.Stop();

Console.WriteLine($"Completed {tasks.Count(t => t.IsCompleted)} in {sw.ElapsedMilliseconds}ms");

Console.WriteLine($"    Success: {tasks.Count(t => t.IsCompletedSuccessfully)}");
Console.WriteLine($"    Failed: {tasks.Count(t => !t.IsCompletedSuccessfully)}");


if (exception != null)
{
    Console.WriteLine(exception.ToString());
}


class SlowContent : HttpContent
{
    private byte[] _buffer;

    public SlowContent(byte[] buffer)
    {
        _buffer = buffer;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        for (int i = 0; i < _buffer.Length;)
        {
            var bytesRemaining = _buffer.Length - i;
            var bytesToWrite = Math.Min(bytesRemaining, Random.Shared.Next(1, 4096));
            var delay = Random.Shared.Next(1, 50);

            await stream.WriteAsync(_buffer, i, bytesToWrite, default);
            await Task.Delay(delay);


            i += bytesToWrite;
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _buffer.Length;
        return true;
    }
}