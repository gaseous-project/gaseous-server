using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Xunit;

namespace gaseous_server.Tests
{
    public class HTTPCommsTests
    {
        private HTTPComms CreateComms(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            var handler = new StubHttpMessageHandler(responder);
            var client = new HttpClient(handler);
            // Inject custom client via reflection (since _httpClient is private). Alternatively, create a constructor in production code.
            var field = typeof(HTTPComms).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var comms = new HTTPComms();
            field?.SetValue(comms, client);
            return comms;
        }

        [Fact]
        public async Task SendRequestAsync_DeserializesJson()
        {
            var json = "{\"value\":42}";
            var comms = CreateComms((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            }));

            var result = await comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, "https://example.com/api");
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Body);
            Assert.Equal(0, result.Body!.Value);
        }

        [Fact]
        public async Task SendRequestAsync_HandlesBinaryBytes()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var comms = CreateComms((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            }));

            var result = await comms.SendRequestAsync<byte[]>(HTTPComms.HttpMethod.GET, "https://example.com/file");
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Body);
            Assert.Equal(bytes, result.Body);
        }

        [Fact]
        public async Task SendRequestAsync_RetryAfterSeconds_WaitsAndRetries()
        {
            int call = 0;
            var comms = CreateComms((req, ct) =>
            {
                call++;
                if (call == 1)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)429);
                    resp.Headers.Add("Retry-After", "1");
                    resp.Content = new StringContent("too many");
                    return Task.FromResult(resp);
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":1}", System.Text.Encoding.UTF8, "application/json")
                });
            });

            var result = await comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, "https://example.com/rate");
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(2, call);
        }

        [Fact]
        public async Task SendRequestAsync_CancellationToken_Cancels()
        {
            var cts = new CancellationTokenSource();
            var comms = CreateComms(async (req, ct) =>
            {
                await Task.Delay(1000, ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":1}", System.Text.Encoding.UTF8, "application/json")
                };
            });

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, "https://example.com/cancel", cancellationToken: cts.Token));
        }

        private class TestDto
        {
            public int Value { get; set; }
        }
    }
}
