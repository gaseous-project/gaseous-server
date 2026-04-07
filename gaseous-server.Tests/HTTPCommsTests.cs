using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private static HTTPComms CreateComms(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            var handler = new StubHttpMessageHandler(responder);
            var client = new HttpClient(handler);
            // Inject custom client via reflection (since _httpClient is private). Alternatively, create a constructor in production code.
            var field = typeof(HTTPComms).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var comms = new HTTPComms();
            field?.SetValue(comms, client);
            return comms;
        }

        private static void SetPrivateStaticIntField(string fieldName, int value)
        {
            var field = typeof(HTTPComms).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, value);
        }

        private static int GetPrivateStaticIntField(string fieldName)
        {
            var field = typeof(HTTPComms).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return field != null ? (int)field.GetValue(null)! : 0;
        }

        [Fact]
        public async Task SendRequestAsync_DeserializesJson()
        {
            var json = "{\"value\":42}";
            var comms = CreateComms((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            }));

            var result = await comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, new Uri("https://example.com/api"));
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Body);
            Assert.Equal(42, result.Body!.Value);
        }

        [Fact]
        public async Task SendRequestAsync_HandlesBinaryBytes()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var comms = CreateComms((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            }));

            var result = await comms.SendRequestAsync<byte[]>(HTTPComms.HttpMethod.GET, new Uri("https://example.com/file"));
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

            var result = await comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, new Uri("https://example.com/rate"));
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(2, call);
        }

        [Fact]
        public async Task SendRequestAsync_CancellationToken_Cancels()
        {
            using var cts = new CancellationTokenSource();
            var comms = CreateComms(async (req, ct) =>
            {
                await Task.Delay(1000, ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":1}", System.Text.Encoding.UTF8, "application/json")
                };
            });

            await cts.CancelAsync();
            await Assert.ThrowsAsync<TaskCanceledException>(() => comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, new Uri("https://example.com/cancel"), cancellationToken: cts.Token));
        }

        [Fact]
        public async Task SendRequestAsync_RequestTimeout_RetryUsesFreshTimeoutWindow()
        {
            int call = 0;
            var comms = CreateComms(async (req, ct) =>
            {
                call++;

                if (call == 1)
                {
                    await Task.Delay(100, ct);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":5}", System.Text.Encoding.UTF8, "application/json")
                };
            });

            var result = await comms.SendRequestAsync<TestDto>(
                HTTPComms.HttpMethod.GET,
                new Uri("https://example.com/timeout-retry"),
                timeout: TimeSpan.FromMilliseconds(20),
                retryCount: 2);

            Assert.Equal(2, call);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Body);
            Assert.Equal(5, result.Body!.Value);
            Assert.Null(result.ErrorType);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task SendRequestAsync_ConcurrentCalls_PreservePerRequestHeaders()
        {
            var seenRequestIds = new ConcurrentBag<string>();

            var comms = CreateComms(async (req, ct) =>
            {
                // Encourage overlap in in-flight requests to exercise thread-safety.
                await Task.Delay(25, ct);

                if (req.Headers.TryGetValues("X-Request-Id", out var values))
                {
                    var id = values.SingleOrDefault();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        seenRequestIds.Add(id);
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":1}", System.Text.Encoding.UTF8, "application/json")
                };
            });

            var expectedIds = Enumerable.Range(1, 20).Select(i => i.ToString()).ToArray();
            var tasks = new List<Task<HTTPComms.HttpResponse<TestDto>>>();

            foreach (var id in expectedIds)
            {
                var headers = new Dictionary<string, string>
                {
                    ["X-Request-Id"] = id
                };

                tasks.Add(comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, new Uri("https://example.com/concurrent"), headers));
            }

            var results = await Task.WhenAll(tasks);

            Assert.All(results, r => Assert.Equal(200, r.StatusCode));
            Assert.Equal(expectedIds.Length, seenRequestIds.Count);
            Assert.Equal(expectedIds.OrderBy(x => x), seenRequestIds.OrderBy(x => x));
        }

        [Fact]
        public async Task SendRequestAsync_Cloudflare1015Text_RetriesWithoutJsonExceptionState()
        {
            int call = 0;
            var comms = CreateComms((req, ct) =>
            {
                call++;
                if (call == 1)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("error code: 1015", System.Text.Encoding.UTF8, "text/plain")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":7}", System.Text.Encoding.UTF8, "application/json")
                });
            });

            // Avoid long waits in the retry branch.
            int originalWait = GetPrivateStaticIntField("_rateLimit429WaitTimeSeconds");
            SetPrivateStaticIntField("_rateLimit429WaitTimeSeconds", 0);
            try
            {
                var result = await comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, new Uri("https://example.com/cf1015"), retryCount: 2);

                Assert.Equal(2, call);
                Assert.Equal(200, result.StatusCode);
                Assert.NotNull(result.Body);
                Assert.Equal(7, result.Body!.Value);
                Assert.Null(result.ErrorType);
                Assert.Null(result.ErrorMessage);
            }
            finally
            {
                SetPrivateStaticIntField("_rateLimit429WaitTimeSeconds", originalWait);
            }
        }

        [Fact]
        public async Task SendRequestAsync_Status420_WaitsAndRetries()
        {
            int call = 0;
            var comms = CreateComms((req, ct) =>
            {
                call++;
                if (call == 1)
                {
                    return Task.FromResult(new HttpResponseMessage((HttpStatusCode)420)
                    {
                        Content = new StringContent("rate limited")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":9}", System.Text.Encoding.UTF8, "application/json")
                });
            });

            // Keep test fast while still exercising 420 retry branch.
            int originalWait = GetPrivateStaticIntField("_rateLimit420WaitTimeSeconds");
            SetPrivateStaticIntField("_rateLimit420WaitTimeSeconds", 0);
            try
            {
                var result = await comms.SendRequestAsync<TestDto>(HTTPComms.HttpMethod.GET, new Uri("https://example.com/420"), retryCount: 2);

                Assert.Equal(2, call);
                Assert.Equal(200, result.StatusCode);
                Assert.NotNull(result.Body);
                Assert.Equal(9, result.Body!.Value);
            }
            finally
            {
                SetPrivateStaticIntField("_rateLimit420WaitTimeSeconds", originalWait);
            }
        }

        private class TestDto
        {
            public int Value { get; set; }
        }
    }
}
